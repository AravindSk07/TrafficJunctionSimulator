using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace JunctionLab
{
    public class RuntimeSmokeTest : MonoBehaviour
    {
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-trafficSmoke")<0) yield break;
            var controller=GetComponent<JunctionController>();
            controller.simulationSpeed=4;
            yield return new WaitForSecondsRealtime(12);
            controller.ToggleRoutes();
            controller.ToggleRoutes();
            controller.ToggleCamera();
            controller.RotateCamera();
            controller.ToggleCamera();
            controller.TogglePause();
            var atPause=controller.Model.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);
            if(controller.Model.Elapsed!=atPause) throw new Exception("Pause failed");
            controller.TogglePause();
            controller.Model.RequestNextPhase();
            yield return new WaitForSecondsRealtime(4);
            var folder=Path.Combine(Application.dataPath,"..","Evidence");
            Directory.CreateDirectory(folder);
            if(controller.Model.Completed==0 || controller.Model.Cars.Count==0) throw new Exception("Runtime traffic failed to progress");
            // Render the actual running scene off-screen: hidden Windows players can have a black backbuffer.
            var camera=controller.sceneCamera;
            var previousRect=camera.rect;
            var target=RenderTexture.GetTemporary(1400,1000,24);
            var previousActive=RenderTexture.active;
            camera.rect=new Rect(0,0,1,1); camera.targetTexture=target;
            camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1400,1000),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder,"scene-preview.png"),image.EncodeToPNG());
            camera.targetTexture=null; camera.rect=previousRect; RenderTexture.active=previousActive;
            RenderTexture.ReleaseTemporary(target); Destroy(image);
            string summary=$"Runtime passed. Completed={controller.Model.Completed}; Active={controller.Model.Cars.Count}; Pause, routes, camera and phase request exercised programmatically.\n";
            float demand=controller.Model.Demand;
            controller.ResetSimulation();
            if(controller.Model.Completed!=0 || controller.Model.Elapsed!=0 || controller.Model.Demand!=demand) throw new Exception("Reset failed");
            File.WriteAllText(Path.Combine(folder,"runtime-check.txt"),summary+"Reset counters and control preservation passed. Scene preview is an off-screen camera render, excluding the IMGUI dashboard.\n");
            Debug.Log("TRAFFIC_RUNTIME_PASS");
            Application.Quit(0);
        }
    }
}
