### Debugging Tips

#### Diagnosing Design-Time Builds

Design-time builds builds are performed behind the scene to gather enough information to populate IntelliSense and the language service.

The results of design-time builds are not visible by default.

To see the results of a design-time build:

1. Under `HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\[VersionHive]\CPS`
set `"Design-time Build Logging"="1"`, where _[VersionHive]_ is the hive you are running under (typically _15.0RoslynDev_ if you are F5'ing from  ProjectSystem.sln)

2. Restart Visual Studio

The results of the design time build will appear in a new catagory called __Build - Design-time__ in the __Output__ window. The verbosity of the catagory respects the settings under __Tools__ -> __Options__ -> __Project and Solutions__ -> __Build and Run__ 