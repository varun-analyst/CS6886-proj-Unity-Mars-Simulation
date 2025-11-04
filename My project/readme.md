Perform the following steps, and paste this code where required and run in order to use Unity:
* Create a Unity project using editor version: **2022.3.62f2**
* Use template: **3D (Built-In Render Pipeline)**
* Install the following from asset store:
    * Procedural Terrain Painter
    * Outdoor Ground Textures
    * Space_Objects
    * Free Rocks
* Then paste the code from "Assets/Editor/AutoSceneGenerator.cs" in to the respective folder.
* Configure if required:
```bash
        // 📸 CONFIGURATION -----------------------------------
        int numScenes = 1000;                // ← Number of samples to generate (up to 10000)
        bool enableTopView = false;          // ← Whether to also render top-down view
        string baseOutputDir = "Assets/TerrainLayerImages";
        // -----------------------------------------------------
```
* Then go to the project and click **Mars** -> **Generate Mars Dataset**

Note: A generated dataset is already here to be tested. It is in `Assets/TerrainLayerImages`