// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.Utilities;
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

static addGithubTrigger(def job, def isPR, def branchName, def jobName, def manualTrigger, def altTriggerPhrase) {
  if (isPR) {
    def triggerCore = "all|${jobName}"

    if (altTriggerPhrase) {
      triggerCore = "${triggerCore}|${altTriggerPhrase}"
    }

    def triggerPhrase = "(?i)^\\s*(@?dotnet-bot\\s+)?(re)?test\\s+(${triggerCore})(\\s+please)?\\s*\$"

    Utilities.addGithubPRTriggerForBranch(job, branchName, jobName, triggerPhrase, manualTrigger)
  } else {
    Utilities.addGithubPushTrigger(job)
  }
}

static addXUnitDotNETResults(def job, def configName) {
  def resultFilePattern = "**/artifacts/${configName}/TestResults/*.xml"
  def skipIfNoTestFiles = false
    
  Utilities.addXUnitDotNETResults(job, resultFilePattern, skipIfNoTestFiles)
}

def createJob(def platform, def configName, def osName, def imageName, def isPR, def manualTrigger, def altTriggerPhrase) {
  def projectName = GithubProject
  def branchName = GithubBranchName  
  def jobName = "${platform}_${configName}"
  def newJob = job(Utilities.getFullJobName(projectName, jobName, isPR))

  Utilities.standardJobSetup(newJob, projectName, isPR, "*/${branchName}")

  addGithubTrigger(newJob, isPR, branchName, jobName, manualTrigger, altTriggerPhrase)
  addArchival(newJob, configName)
  addXUnitDotNETResults(newJob, configName)
  Utilities.setMachineAffinity(newJob, osName, imageName)

  return newJob
}

def osName = "Windows_NT"
def imageName = "latest-dev15-3"

[true, false].each { isPR ->
  ["debug", "release"].each { configName ->
    
    def platform = "windows"
    def manualTrigger = false
    def altTriggerPhrase = ""

    def newJob = createJob(platform, configName, osName, imageName, isPR, manualTrigger, altTriggerPhrase)

    newJob.with {
      wrappers {
        credentialsBinding {
          string("CODECOV_TOKEN", "CODECOV_TOKEN_DOTNET_PROJECT_SYSTEM")
        }
      }
      steps {
        batchFile(".\\build\\CIBuild.cmd -configuration ${configName} -prepareMachine")
      }
    }
  }
}

[true, false].each { isPR ->
  ["debug", "release"].each { configName ->
    
    def platform = "windows_integration"
    def manualTrigger = true
    def altTriggerPhrase = "vsi"
    
    def newJob = createJob(platform, configName, osName, imageName, isPR, manualTrigger, altTriggerPhrase)

    newJob.with {
      wrappers {
        credentialsBinding {
          string("CODECOV_TOKEN", "CODECOV_TOKEN_DOTNET_PROJECT_SYSTEM")
        }
      }
      steps {
        batchFile(".\\build\\VSIBuild.cmd -configuration ${configName} -prepareMachine")
      }
    }
  }
}