// Import the utility functionality.
import jobs.generation.*;
import jobs.generation.InternalUtilities;

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
                batchFile("build.cmd /${configuration.toLowerCase()}")
            }
        }

        Utilities.addArchival(newJob, "bin/**/*" /* filesToArchive */, "bin/obj/**" /* filesToExclude */, true /* doNotFailIfNothingArchived */ , false /* archiveOnlyIfSuccessful */)
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto-dev15')
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
    label('windows-roslyn-internal-dev15-project-system')

    // This opens the set of build steps that will be run.
    steps {
        // Build roslyn-project-system repo.
        batchFile("build.cmd /release")

        // git clone roslyn-internal to build and run VSI tao tests.
        batchFile("""git clone https://github.com/dotnet/roslyn-internal.git
pushd roslyn-internal
git submodule init
git submodule sync
git submodule update --init --recursive

set TEMP=%WORKSPACE%\\roslyn-internal\\Open\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%

BuildAndTest.cmd -build:true -clean:false -deployExtensions:false -trackFileAccess:false -officialBuild:false -realSignBuild:false -parallel:true -release:true -delaySignBuild:true -dependencies:true -samples:false -devDivInsertionFiles:false -unit:false -eta:false -vs:true -cibuild:true -x64:false -netcoretestrun
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

  Utilities.addArchival(myJob, archiveSettings)
}

addVsiArchive(newVsiJob)

// For now, trigger VSI jobs only on explicit request.
Utilities.standardJobSetup(newVsiJob, project, false /* isPr */, "*/${branch}")
Utilities.addGithubPushTrigger(newVsiJob)
Utilities.addHtmlPublisher(newVsiJob, "roslyn-internal/Open/Binaries/Release/VSIntegrationTestLogs", 'VS Integration Test Logs', '*.html')

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.