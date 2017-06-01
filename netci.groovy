// Import the utility functionality.
import jobs.generation.*;

// Defines a the new of the repo, used elsewhere in the file
def project = GithubProject
def branch = GithubBranchName

// Generate the builds for debug and release, commit and PRJob
[true, false].each { isPR -> // Defines a closure over true and false, value assigned to isPR
    ['Debug', 'Release'].each { configuration ->

        def newJobName = Utilities.getFullJobName(project, "windows_${configuration.toLowerCase()}", isPR)

        def newJob = job(newJobName) {
            // This opens the set of build steps that will be run.
            steps {
                // Indicates that a batch script should be run with the build string (see above)
                // Also available is:
                // shell (for unix scripting)
                batchFile("""
echo *** Build Roslyn Project System ***
SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\

build.cmd /no-node-reuse /no-deploy-extension /${configuration.toLowerCase()}
""")
            }
        }

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles("bin/**/*")
        archiveSettings.excludeFiles("bin/obj/*")
        archiveSettings.setFailIfNothingArchived()
        archiveSettings.setArchiveOnFailure()
        Utilities.addArchival(newJob, archiveSettings)
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto-dev15-0')
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        Utilities.addXUnitDotNETResults(newJob, "**/*TestResults.xml")
        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "Windows ${configuration}")
        }
        else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}

// Add VSI jobs.
// Generate the builds for commit and PRJob
[true, false].each { isPR -> // Defines a closure over true and false, value assigned to isPR
    ['Debug', 'Release'].each { configuration ->

        def newVsiJobName = Utilities.getFullJobName(project, "windows_integration_${configuration.toLowerCase()}", isPR)
        
        def newVsiJob = job(newVsiJobName) {
            // This opens the set of build steps that will be run.
            steps {             
                // Indicates that a batch script should be run with the build string (see above)
                // Also available is:
                // shell (for unix scripting)
                batchFile("""
echo *** Installing 1.0 CLI ***

@powershell -NoProfile -ExecutionPolicy Bypass -Command "((New-Object System.Net.WebClient).DownloadFile('https://download.microsoft.com/download/B/9/F/B9F1AF57-C14A-4670-9973-CDF47209B5BF/dotnet-dev-win-x64.1.0.4.exe', 'dotnet-dev-win-x64.1.0.4.exe'))"
dotnet-dev-win-x64.1.0.4.exe /install /quiet /norestart /log bin\\cli_install.log

echo *** Build Roslyn Project System ***
SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\Preview\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\Preview\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\Preview\\Enterprise\\VSSDK\\

build.cmd /no-node-reuse /no-deploy-extension /skiptests /integrationtests /${configuration.toLowerCase()}
""")
            }
        }

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles("bin/**/*")
        archiveSettings.excludeFiles("bin/obj/*")
        archiveSettings.setFailIfNothingArchived()
        archiveSettings.setArchiveOnFailure()
        Utilities.addArchival(newVsiJob, archiveSettings)
        Utilities.setMachineAffinity(newVsiJob, 'Windows_NT', 'latest-dev15-3-preview1')
        Utilities.standardJobSetup(newVsiJob, project, isPR, "*/${branch}")
        Utilities.addXUnitDotNETResults(newVsiJob, "**/*TestResults.xml")

        if (isPR) {
            def triggerPhrase = generateTriggerPhrase(newVsiJobName, "vsi")
            Utilities.addGithubPRTriggerForBranch(newVsiJob, branch, newVsiJobName, triggerPhrase, /*triggerPhraseOnly*/ true)
        } else {
            Utilities.addGithubPushTrigger(newVsiJob)
        }
    }
}

static String generateTriggerPhrase(String jobName, String triggerPhraseExtra) {
    def triggerCore = "all|${jobName}"
    if (triggerPhraseExtra) {
        triggerCore = "${triggerCore}|${triggerPhraseExtra}"
    }
    return "(?i).*test\\W+(${triggerCore})\\W+please.*";
}

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
