---
name: Branch checklist
about: An outline of the steps needed to create a new branch (internal use)
title: Create [BRANCH NAME] branch checklist
labels: Area-Infrastructure
assignees: ''

---

_Descriptions of these steps can be found in the team OneNote._

<!-- Replace all ❓ characters as you work through this. -->

- [ ] Identify base commit for new branch
  - [ ] According to the [schedule](https://dev.azure.com/devdiv/DevDiv/_wiki/wikis/DevDiv.wiki/10097/Dev17-Release), `main` snapped to `rel/d17.❓` at ❓ PST
  - [ ] Last [completed Project System insertion PR](https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequests?_a=completed&createdBy=9f64bc2f-479b-429f-a665-fec80e130b1f&assignedTo=6e89082d-fdd2-4442-a310-051df5bdc73c): https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/❓
  - [ ] Last inserted GitHub PR: https://github.com/dotnet/project-system/pull/❓
  - [ ] Merge commit of that PR: ❓
- [ ] Create branch on GitHub at that commit
  - [ ] https://github.com/dotnet/project-system/tree/dev17.❓.x
- [ ] For branches not matching `dev*` or `feature/*` (usually we skip these)
  - [ ] Update the [YAML file](https://github.com/dotnet/project-system/blob/main/build/ci/unit-tests.yml) to support CI/PR builds
  - [ ] Update the [signed build definition](https://devdiv.visualstudio.com/DevDiv/_build?definitionId=9675) to build the new branch
- [ ] Update Roslyn Tools [config.xml](https://github.com/dotnet/roslyn-tools/blob/main/src/GitHubCreateMergePRs/config.xml) file to flow branch changes to the latest dev branch
  - [ ] dotnet/roslyn-tools PR: https://github.com/dotnet/roslyn-tools/pull/❓
- [ ] Update `Versions.props` so `<ProjectSystemVersion>` matches the version of VS, if needed
    - [ ] In new branch: https://github.com/dotnet/project-system/blob/dev17.❓.x/build/import/Versions.props
    - [ ] In `main`: https://github.com/dotnet/project-system/blob/main/build/import/Versions.props
- [ ] Clone existing release definition to insert this branch into VS (see OneNote)
- [ ] Update [README.md](https://github.com/dotnet/project-system/blob/main/README.md) (in `main`) if we need new badges
- [ ] Update [MSFTBot milestone tracking](https://aka.ms/fabricbotconfig)
