// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.Utilities;

static addArchival(def job) {
  def archivalSettings = new ArchivalSettings()
  archivalSettings.addFiles("**/artifacts/**")
  archivalSettings.excludeFiles("**/artifacts/${configName}/obj/**")
  archivalSettings.setFailIfNothingArchived()
  archivalSettings.setArchiveOnFailure()

  Utilities.addArchival(job, archivalSettings)
}

static addGithubTrigger(def job, def isPR, def branchName, def jobName) {
  if (isPR) {
    def prContext = "prtest/${jobName.replace('_', '/')}"
    def triggerPhrase = "(?i)^\\s*(@?dotnet-bot\\s+)?(re)?test\\s+(${prContext})(\\s+please)?\\s*\$"
    def triggerOnPhraseOnly = false

    Utilities.addGithubPRTriggerForBranch(job, branchName, prContext, triggerPhrase, triggerOnPhraseOnly)
  } else {
    Utilities.addGithubPushTrigger(job)
  }
}

static addXUnitDotNETResults(def job, def configName) {
  def resultFilePattern = "**/artifacts/${configName}/TestResults/*.xml"
  def skipIfNoTestFiles = false
    
  Utilities.addXUnitDotNETResults(job, resultFilePattern, skipIfNoTestFiles)
}

static createJob(def platform, def configName, def isPR) {
  def projectName = GithubProject
  def branchName = GithubBranchName

  def jobName = "${platform}_${configName}"
  def newJob = job(Utilities.getFullJobName(projectName, jobName, isPR))

  Utilities.standardJobSetup(newJob, projectName, isPR, "*/${branchName}")

  addGithubTrigger(newJob, isPR, branchName, jobName)
  addArchival(newJob)
  addXUnitDotNETResults(newJob, configName)

  return newJob
}

[true, false].each { isPR ->
  ['windows', 'windows_integration'].each { platform ->
    ['debug', 'release'].each { configName ->
  
      def newJob = createJob(platform, configName, isPR)

      Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-dev15-3-preview2')

      newJob.with {
        steps {
          if (platform == 'windows') {
            batchFile(".\\CIBuild.cmd -configuration ${configName} -prepareMachine")
          } else if (platform == 'windows_integration') {
            batchFile(".\\VSIBuild.cmd -configuration ${configName} -prepareMachine")
          }
        }
      }
    }
  }
}