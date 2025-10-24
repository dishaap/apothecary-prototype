<h1>Documentation & Troubleshooting</h1>

This project is for the Unity Project of the as-yet unnamed Apothecary game that we are prototyping. It stores Unity assets, packages and documentation required for version control. 

<h2>File Tree & Organization</h2>

The project follows <a href="https://unity.com/how-to/organizing-your-project">Unity conventions for organizing projects</a>, which is a derivation of C# project organization conventions. In short this means that:
<ul>
  <li>All Unity assets must go in the <code>Assets</code> folder, and every asset is further classified into subfolders by type, location used, subtype in that order. (eg: if a background music for a kitchen scene needs to be stored, it would be under <code>Assets > Audio > Kitchen > Background</code>.)</li>
  <li>All names are written in CamelCase. If the name needs to specify a certain classification that distinguishes the asset from other assets of the same subtype, separate that classifier with an underscore. (eg: if the background music is classical, it can be named <code>BackgroundMusic_Classical</code>.)</li>
  <li>Any file that is not required for Unity but is needed in the repository should be a file or folder at the same level as the Unity project <code>SomeMagicalFairyApothecary</code> and should not be added within that folder. All files within the Unity project must be utilized for running it.</li>
  <li>Within Scenes, all GameObjects must be organized hierarchically, with collections of similar objects having a parent GameObject for better organization and handling. (eg: all collectible items should be under parent GameObject <code>Collectibles</code>.)</li>
</ul>

This is the current hierarchy of the project, representing only the first four levels of organization. (TBA: Assets organization)

<pre>
  +-- apothecary-prototype (Repository)
  |  +-- SomeMagicalFairyApothecary (Unity Project)
  |  |  +-- Assets (All assets to be used in compiling Unity Project)
  |  |  +-- Packages (Specs for all packages required to compile project, as .json files)
  |  |  +-- ProjectSettings (Configurations for running the Unity project and managing assets, as .asset files)
  |  |  |-- .gitattributes (Configurations for the git project)
  |  |  |-- .gitignore
  |  |  |-- .vsconfig (Visual Studio configuration)
  |  |-- README
</pre>

<h2>Project Configuration & Troubleshooting</h2>

<a href="https://unity.com/download">Unity 6.0 or higher</a> is required to run this project. The Unity Editor can be downloaded via the Unity Hub and by default, Unity also installs Visual Studio if it's not on your system as it's the preferred IDE for Unity Scripting. (<a href="https://learn.microsoft.com/en-us/visualstudio/gamedev/unity/get-started/getting-started-with-visual-studio-tools-for-unity?pivots=windows">Tutorial on configuring VS for Unity</a>!)

After installing Unity, clone this repository. Once this repository is available locally, locate the folder via Unity Hub by clicking Add (top right corner) > Add from Disk and open the <code>SomeMagicalFairyApothecary</code> folder as your project. Unity should recognize it and open it in the editor. 

<h3>Temporary and Large Files</h3>
This Git repository is already set up to track files required to collaborate on a Unity project, and will ignore all temporary files (as specified in <code>.gitignore</code>). To add a file type or folder to be ignored, open the existing <code>.gitignore</code> in a text editor to edit it with the new file type or folder name.<br /><br />

Sometimes, Git will refuse to commit files due their size exceeding 100MB. To override this, the project has been configured with Git LFS (Large File Storage), and any specific file name/general file type can be added to the LFS tracking by adding it through the cmd line at the repository root:

<pre># specific file
git lfs track "filename.type"

# file type
git lfs track "*.type"</pre>

The file name or type should be added to <code>.gitattributes</code>

<h2>Repository Organization</h2>

To avoid as many merge conflicts as possible, for development, branches are created from main per feature developed. 

For example, if the home screen feature is under development, all commits related to it would be under the branch <code>home-screen</code>, until development of the feature is finished, and the branch can be merged. If there is a sub-feature to be added specifically for the home screen, it can be developed on a sub-branch <code>home-screen--\<sub-feature-name\></code> and merged into <code>home-screen</code>. 

The feature branch development method helps encapsulate the project most efficiently and is a derivative of <a href="https://www.atlassian.com/git/tutorials/comparing-workflows/feature-branch-workflow">industry standard version control</a>. The feature branches are owned by the user who created them but can be checked out by any other user, without loss of progress on their own branches and minimal code conflicts.

Once development on a feature is complete, a Pull Request can be filed for review by another team member before merging into main. Once the PR is accepted, the feature will be added to the main workflow of the project. The feature branch can then be closed to avoid clogging the workflow of the repository with dead branches.
