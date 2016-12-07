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
                batchFile("""SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\

build.cmd /no-deploy-extension /${configuration.toLowerCase()}""")
            }
        }

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles("bin/**/*")
        archiveSettings.excludeFiles("bin/obj/*")
        archiveSettings.setFailIfNothingArchived()
        archiveSettings.setArchiveOnFailure()
        Utilities.addArchival(newJob, archiveSettings)
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto-dev15-rc')
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

// Add VSI job.
// For now, trigger VSI jobs only on explicit request.
def newVsiJobName = Utilities.getFullJobName(project, "vsi", false /* isPr */)

def newVsiJob = job(newVsiJobName) {
    description('')

    // This opens the set of build steps that will be run.
    steps {
        // Build roslyn-project-system repo - we also need to set certain environment variables for building the repo with VS15 toolset.
        batchFile("""SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\

build.cmd /release""")

        // Build roslyn-internal and run netcore VSI tao tests.
        batchFile("""SET VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\Common7\\Tools\\
SET VSSDK150Install=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\
SET VSSDKInstall=%ProgramFiles(x86)%\\Microsoft Visual Studio\\2017\\Enterprise\\VSSDK\\

pushd %WORKSPACE%\\roslyn-internal
git submodule init
git submodule sync
git submodule update --init --recursive

set TEMP=%WORKSPACE%\\roslyn-internal\\Open\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%

set EchoOn=true

BuildAndTest.cmd -build:true -clean:false -deployExtensions:true -trackFileAccess:false -officialBuild:false -realSignBuild:false -parallel:true -release:true -delaySignBuild:true -samples:false -unit:false -eta:false -vs:true -cibuild:true -x64:false -netcoretestrun
popd""")
    }
}

// Archive VSI artifacts.
static void addVsiArchive(def myJob) {
  def archiveSettings = new ArchivalSettings()
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.pdb')
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.xml')
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.log')
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.zip')
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.png')
  archiveSettings.addFiles('roslyn-internal/Open/Binaries/**/*.xml')
  archiveSettings.excludeFiles('roslyn-internal/Open/Binaries/Obj/**')
  archiveSettings.excludeFiles('roslyn-internal/Open/Binaries/Bootstrap/**')

  archiveSettings.setArchiveOnFailure()
  archiveSettings.setFailIfNothingArchived()
  Utilities.addArchival(myJob, archiveSettings)
}

// ISSUE: Temporary until a full builder for multi-scm source control is available.
// Replace the scm settings with a multiScm setup.  Note that this will not work for PR jobs
static void addVsiMultiScm(def myJob, def project) {
    myJob.with {
        multiscm {
            git {
                remote {
                    // Use the input project
                    github(project)
                }
                // Pull from the desired branch input branch passed as a parameter (set up by standardJobSetup)
                branch('${GitBranchOrCommit}')
            }
            git {
                remote {
                    url('https://github.com/dotnet/roslyn-internal')
                    credentials('dotnet-bot-private-repo-token')
                }
                extensions {
                    relativeTargetDirectory('roslyn-internal')
                }
                // roslyn-internal - pull in a specific LKG commit from master.
                // In future, '*/master' can be placed here to pull latest sources.
                branch('a8f887e56a98e173f9049d176f3996f3a6171c27')
            }
        }
    }
}
// END ISSUE

addVsiArchive(newVsiJob)
Utilities.setMachineAffinity(newVsiJob, 'Windows_NT', 'latest-or-auto-dev15-internal')
// For now, trigger VSI jobs only on explicit request.
Utilities.standardJobSetup(newVsiJob, project, false /* isPr */, "*/${branch}")
// ISSUE: Temporary until a full builder for source control is available.
addVsiMultiScm(newVsiJob, project)
Utilities.addGithubPushTrigger(newVsiJob)
Utilities.addHtmlPublisher(newVsiJob, "roslyn-internal/Open/Binaries/Release/VSIntegrationTestLogs", 'VS Integration Test Logs', '*.html')

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
