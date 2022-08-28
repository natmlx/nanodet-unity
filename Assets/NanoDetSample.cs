/* 
*   NanoDet
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using UnityEngine;
    using NatML.Devices;
    using NatML.Devices.Outputs;
    using NatML.Features;
    using NatML.Vision;
    using NatML.Visualizers;

    public sealed class NanoDetSample : MonoBehaviour {

        [Header(@"UI")]
        public NanoDetVisualizer visualizer;

        CameraDevice cameraDevice;
        TextureOutput textureOutput;

        MLModelData modelData;
        MLModel model;
        NanoDetPredictor predictor;

        async void Start () {
            // Request permissions
            var permissionStatus = await MediaDeviceQuery.RequestPermissions<CameraDevice>();
            if (permissionStatus != PermissionStatus.Authorized) {
                Debug.LogError(@"User did not grant camera permissions");
                return;
            }
            // Discover the camera
            var query = new MediaDeviceQuery(MediaDeviceCriteria.CameraDevice);
            cameraDevice = query.current as CameraDevice;
            // Start the preview
            textureOutput = new TextureOutput();
            cameraDevice.StartRunning(textureOutput);
            // Display the camera preview
            var previewTexture = await textureOutput;
            visualizer.Render(previewTexture);
            // Create the NanoDet predictor
            modelData = await MLModelData.FromHub("@natsuite/nanodet");
            model = modelData.Deserialize();
            predictor = new NanoDetPredictor(model, modelData.labels);
        }

        void Update () {
            // Check that predictor has been loaded
            if (predictor == null)
                return;
            // Create input feature
            var inputFeature = new MLImageFeature(textureOutput.texture);
            (inputFeature.mean, inputFeature.std) = modelData.normalization;
            inputFeature.aspectMode = modelData.aspectMode;
            // Detect
            var detections = predictor.Predict(inputFeature);
            // Visualize
            visualizer.Render(textureOutput.texture, detections);
        }

        void OnDisable () => model?.Dispose();
    }
}