using System;
using System.IO;
using JunctionLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace JunctionLab.Editor
{
    public static class ProjectBuilder
    {
        const string Root="Assets/Traffic/";
        static Material road, sidewalk, grass, white, dark, glass, wood, leaf;
        static GameObject scenery;

        static Material Mat(string name, Color color)
        {
            var material=new Material(Shader.Find("Standard")){color=color};
            material.SetFloat("_Glossiness",.15f);
            AssetDatabase.CreateAsset(material,Root+"Materials/"+name+".mat");
            return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material,Transform parent=null,PrimitiveType type=PrimitiveType.Cube)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=name;
            obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static GameObject SavePrefab(GameObject obj,string name)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Root+"Prefabs/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(obj); return prefab;
        }
        static GameObject Car(string name,Material body,bool van)
        {
            var root=new GameObject(name);
            Box("Body",new Vector3(0,.65f,0),new Vector3(1.75f,.55f,3.6f),body,root.transform);
            Box("Cabin",new Vector3(0,1.15f,van?-.2f:-.3f),new Vector3(1.5f,van?.85f:.55f,van?2.6f:1.8f),glass,root.transform);
            Box("Roof",new Vector3(0,van?1.62f:1.46f,-.3f),new Vector3(1.55f,.12f,van?2.6f:1.8f),body,root.transform);
            for(int side=-1;side<=1;side+=2) for(int axle=-1;axle<=1;axle+=2)
            {
                var wheel=Box("Wheel",new Vector3(side*.85f,.42f,axle*1.05f),new Vector3(.62f,.16f,.62f),dark,root.transform,PrimitiveType.Cylinder);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Box("Headlight",new Vector3(side*.57f,.71f,1.82f),new Vector3(.4f,.18f,.04f),white,root.transform);
                Box("Tail light",new Vector3(side*.6f,.71f,-1.82f),new Vector3(.3f,.16f,.04f),Mat(name+"Tail"+side,new Color(.9f,.19f,.14f)),root.transform);
            }
            return SavePrefab(root,name);
        }

        [MenuItem("Junction Lab/Generate Demo (new project only)")]
        public static void Generate()
        {
            if(File.Exists(Root+"Scenes/TrafficJunction.unity")) throw new InvalidOperationException("Demo already exists. Generation intentionally does not overwrite authored assets.");
            foreach(var path in new[]{"Materials","Prefabs","Scenes","Settings"}) Directory.CreateDirectory(Root+path);
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            road=Mat("Asphalt",new Color(.18f,.23f,.28f));
            sidewalk=Mat("Sidewalk",new Color(.75f,.8f,.78f));
            grass=Mat("Park",new Color(.35f,.59f,.43f));
            white=Mat("Ivory",new Color(.93f,.92f,.82f));
            dark=Mat("Charcoal",new Color(.08f,.13f,.18f));
            glass=Mat("Glazing",new Color(.17f,.36f,.42f));
            wood=Mat("TreeTrunk",new Color(.35f,.24f,.17f));
            leaf=Mat("Foliage",new Color(.19f,.43f,.31f));
            var blue=Mat("OceanBlue",new Color(.18f,.57f,.73f));
            var orange=Mat("Marigold",new Color(.98f,.58f,.24f));
            var cream=Mat("WarmWhite",new Color(.85f,.87f,.79f));
            var settings=ScriptableObject.CreateInstance<TrafficSettings>();
            AssetDatabase.CreateAsset(settings,Root+"Settings/DefaultTraffic.asset");
            var cars=new[]{Car("CompactBlue",blue,false),Car("TaxiGold",orange,false),Car("DeliveryVan",cream,true)};
            scenery=new GameObject("Downtown - original low-poly environment");
            Box("Island",new Vector3(0,-.8f,0),new Vector3(79,1.5f,79),grass,scenery.transform);
            Box("NorthSouthRoad",new Vector3(0,0,0),new Vector3(12,.1f,78),road,scenery.transform);
            Box("EastWestRoad",new Vector3(0,.01f,0),new Vector3(78,.1f,12),road,scenery.transform);
            var yellow=Mat("LaneGold",new Color(.99f,.75f,.35f));
            for(int direction=0;direction<4;direction++)
            {
                var arm=new GameObject("Road markings "+direction); arm.transform.SetParent(scenery.transform);
                arm.transform.rotation=Quaternion.Euler(0,direction*90,0);
                for(float z=-36;z<-11;z+=4)
                    Box("Center dash",new Vector3(0,.08f,z),new Vector3(.14f,.03f,2),yellow,arm.transform);
                Box("Stop line",new Vector3(3,.09f,-11.3f),new Vector3(5.4f,.03f,.32f),white,arm.transform);
                for(float x=-5;x<6;x+=1.4f)
                    Box("Crosswalk",new Vector3(x,.08f,-9.5f),new Vector3(.7f,.03f,2),white,arm.transform);
                Box("Edge left",new Vector3(-5.6f,.07f,-25),new Vector3(.1f,.03f,26),white,arm.transform);
                Box("Edge right",new Vector3(5.6f,.07f,-25),new Vector3(.1f,.03f,26),white,arm.transform);
            }
            var treeRoot=new GameObject("Tree");
            Box("Trunk",new Vector3(0,1.1f,0),new Vector3(.5f,2.2f,.5f),wood,treeRoot.transform);
            Box("Crown",new Vector3(0,2.8f,0),new Vector3(2.8f,3,2.8f),leaf,treeRoot.transform,PrimitiveType.Sphere);
            var tree=SavePrefab(treeRoot,"ParkTree");
            var buildingRoot=new GameObject("Building");
            Box("Facade",new Vector3(0,3.2f,0),new Vector3(9,6.4f,8),cream,buildingRoot.transform);
            Box("Roof",new Vector3(0,6.5f,0),new Vector3(9.4f,.25f,8.4f),dark,buildingRoot.transform);
            for(int floor=0;floor<2;floor++) for(int x=-3;x<=3;x+=2)
            {
                Box("WindowFront",new Vector3(x,2+floor*2.5f,-4.03f),new Vector3(1.1f,1.45f,.08f),glass,buildingRoot.transform);
                Box("WindowBack",new Vector3(x,2+floor*2.5f,4.03f),new Vector3(1.1f,1.45f,.08f),glass,buildingRoot.transform);
            }
            var building=SavePrefab(buildingRoot,"CityBuilding");
            for(int xsign=-1;xsign<=1;xsign+=2) for(int zsign=-1;zsign<=1;zsign+=2)
            {
                Box("Pavement block",new Vector3(xsign*23,.12f,zsign*23),new Vector3(31,.3f,31),sidewalk,scenery.transform);
                Box("Garden",new Vector3(xsign*14,.3f,zsign*14),new Vector3(9,.08f,9),grass,scenery.transform);
                for(int n=0;n<3;n++)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(tree,scenery.transform);
                    instance.transform.position=new Vector3(xsign*(11+n*3),.35f,zsign*(12+(n%2)*3));
                }
                var b=(GameObject)PrefabUtility.InstantiatePrefab(building,scenery.transform);
                b.transform.position=new Vector3(xsign*27,.3f,zsign*24);
                b.transform.localScale=new Vector3(1, xsign==zsign?1:1.6f,1.3f);
                b.transform.rotation=Quaternion.Euler(0,xsign*90,0);
                var b2=(GameObject)PrefabUtility.InstantiatePrefab(building,scenery.transform);
                b2.transform.position=new Vector3(xsign*17,.3f,zsign*32);
                b2.transform.localScale=new Vector3(.8f,.7f,.8f);
                for(int n=0;n<3;n++)
                {
                    Box("Parking bay",new Vector3(xsign*(23+n*3),.31f,zsign*12),new Vector3(.12f,.03f,5),white,scenery.transform);
                }
            }
            var go=new GameObject("Junction Simulation");
            var controller=go.AddComponent<JunctionController>(); controller.settings=settings; controller.vehiclePrefabs=cars;
            go.AddComponent<TrafficDashboard>().junction=controller;
            go.AddComponent<RuntimeSmokeTest>();
            controller.redLights=new Renderer[4]; controller.amberLights=new Renderer[4]; controller.greenLights=new Renderer[4];
            var lampMaterial=Mat("SignalLens",Color.white); lampMaterial.EnableKeyword("_EMISSION");
            for(int a=0;a<4;a++)
            {
                var signal=new GameObject("Signal "+a); signal.transform.SetParent(scenery.transform);
                signal.transform.rotation=Quaternion.Euler(0,a*90,0);
                Box("Pole",new Vector3(6.7f,2.3f,-7),new Vector3(.22f,4.6f,.22f),dark,signal.transform);
                Box("Housing",new Vector3(6.7f,4.2f,-7),new Vector3(.9f,2.1f,.6f),dark,signal.transform);
                var renderers=new Renderer[3];
                for(int lamp=0;lamp<3;lamp++)
                    renderers[lamp]=Box("Lens "+lamp,new Vector3(6.7f,4.85f-lamp*.65f,-7.35f),Vector3.one*.47f,lampMaterial,signal.transform,PrimitiveType.Sphere).GetComponent<Renderer>();
                controller.redLights[a]=renderers[0]; controller.amberLights[a]=renderers[1]; controller.greenLights[a]=renderers[2];
            }
            var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.78f,.85f,.86f);
            camera.orthographic=true; camera.orthographicSize=45; camera.nearClipPlane=.1f; camera.farClipPlane=250;
            camera.transform.position=new Vector3(35,55,-45); camera.transform.LookAt(Vector3.zero);
            controller.sceneCamera=camera;
            var sun=new GameObject("Afternoon sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(48,-35,0); sun.intensity=1.1f; sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.72f,.78f,.82f);
            QualitySettings.shadowDistance=160; QualitySettings.antiAliasing=4;
            PlayerSettings.companyName="Junction Lab"; PlayerSettings.productName="Traffic Junction Simulator";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"Scenes/TrafficJunction.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/TrafficJunction.unity",true)};
            AssetDatabase.SaveAssets();
            Debug.Log("TRAFFIC_GENERATION_PASS");
        }

        public static void FinalizeAndVerify()
        {
            var scene=EditorSceneManager.OpenScene(Root+"Scenes/TrafficJunction.unity");
            foreach(var item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(item.name=="Stop line" && item.parent!=null && item.parent.name.StartsWith("Road markings"))
                    item.localPosition=new Vector3(3,.09f,-11.3f);
            EditorSceneManager.SaveScene(scene);
            SimulationChecks.Run();
            BuildWindows();
        }

        public static void BuildWindows()
        {
            var folder=Path.GetFullPath("Builds/Windows"); Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,folder+"/TrafficJunctionSimulator.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Player build failed: "+report.summary.result);
            Debug.Log("TRAFFIC_BUILD_PASS");
        }
    }
}
