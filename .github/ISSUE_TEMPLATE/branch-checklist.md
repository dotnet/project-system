---
name: Branch checklist
about: An outline of the steps needed to create a new branch
title: Create [BRANCH NAME] branch checklist
labels: Area-Infrastructure
assignees: ''

---

_Descriptions of these steps can be found in the team OneNote._

- [ ] Identify base commit for new branch
- [ ] Create branch on GitHub
- [ ] Update the YAML file to support CI/PR builds
- [ ] Update Roslyn Tools config.xml file to flow branch changes to the latest dev branch
- [ ] Update Versions.props so ProjectSystemVersion matches the version of VS
- [ ] Update the signed build definition to build the new branch
- [ ] Clone existing release definition to insert this branch into VS
- [ ] Update README.md if we need new badges
- [ ] Update Versions.props in main to reflect the version targeted by that branch
- [ ] Update MSFTBot milestone tracking
