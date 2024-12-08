# Mesh Editor

To create bezier meshes, I developed a mesh editor plugging for Unity. It should be able to open under “Window/Mesh Editor” in the toolbar.

Some features:

- Use Catmull-Rom patches to create all the curvature surfaces. This is easier to use than arbitrary Bezier patches, for it interpolates all the points and has first-order continuity.
- Besides the basic surface primitive patch, I also managed to create closed surface patches.
- I also developed multiple tools to manipulate the control points.
- You can use left drag/click to select control points. Use ‘g’ to toggle enability.
- The ‘Save’ button will save the current mesh to the ‘Assets/Temp’ directory.
