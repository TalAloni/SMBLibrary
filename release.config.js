const { version: previousVersion } = require("./package.json");
const projectfolder = "SMBLibrary/";
const csprojfile = "Lansweeper.SMBLibrary.csproj";
const nugetpackage = "Lansweeper.SMBLibrary";
const gitprimarybranch = "master";

module.exports = {
  "branches": [
    gitprimarybranch
  ],
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    "@semantic-release/npm",
    [
      "@semantic-release/exec",
      {
        "prepareCmd": `sed -i -E 's/<Version>${previousVersion}/<Version>\${nextRelease.version}/' ${projectfolder}${csprojfile} && \
          git add ${projectfolder}${csprojfile} && \
          git commit -m ":bookmark: Bump from ${previousVersion} to \${nextRelease.version} in ${projectfolder}${csprojfile}" && \
          dotnet pack ${projectfolder}${csprojfile} --configuration Release && \
          dotnet nuget push ./${projectfolder}bin/Release/${nugetpackage}.\${nextRelease.version}.nupkg --source \"github\" --api-key $GITHUB_TOKEN`
      }
    ],
    [
      "@semantic-release/git",
      {
        "assets": [
          "CHANGELOG.md",
          "package.json"
        ],
        "message": ":bookmark: Release ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}"
      }
    ],
    "@semantic-release/github",
    [
      "@semantic-release/changelog",
      {
        "changelogFile": "CHANGELOG.md"
      }
    ]
  ]
}