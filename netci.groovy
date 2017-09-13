// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.Utilities;
import jobs.generation.InternalUtilities;
import jobs.generation.ArchivalSettings;

static addArchival(def job, def configName) {
  def archivalSettings = new ArchivalSettings()
  archivalSettings.addFiles("**/artifacts/**")
  archivalSettings.excludeFiles("**/artifacts/${configName}/obj/**")
  archivalSettings.excludeFiles("**/artifacts/${configName}/tmp/**")
  archivalSettings.excludeFiles("**/artifacts/${configName}/VSSetup.obj/**")
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

def createJob(def platform, def configName, def isPR) {
  def projectName = GithubProject
  def branchName = GithubBranchName  
  def jobName = "${platform}_${configName}"
  def newJob = job(InternalUtilities.getFullJobName(projectName, jobName, isPR))

  InternalUtilities.standardJobSetup(newJob, projectName, isPR, "*/${branchName}")

  addGithubTrigger(newJob, isPR, branchName, jobName)
  addArchival(newJob, configName)

  return newJob
}

[true, false].each { isPR ->
  ['windows'].each { platform ->
    ['debug', 'release'].each { configName ->
      def newJob = createJob(platform, configName, isPR)

      Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-dev15-3')

      newJob.with {
        steps {
          batchFile(".\\build\\CIBuild.cmd -configuration ${configName} -prepareMachine")
        }
      }
    }
  }
}
