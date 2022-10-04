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
    using Visualizers;

    public sealed class NanoDetSample : MonoBehaviour {

        [Header(@"UI")]
        public NanoDetVisualizer visualizer;

        private CameraDevice cameraDevice;
        private TextureOutput cameraTextureOutput;

        private MLModelData modelData;
        private MLModel model;
        private NanoDetPredictor predictor;

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
            cameraTextureOutput = new TextureOutput();
            cameraDevice.StartRunning(cameraTextureOutput);
            // Display the camera preview
            var cameraTexture = await cameraTextureOutput;
            visualizer.image = cameraTexture;
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
            var imageFeature = new MLImageFeature(cameraTextureOutput.texture);
            (imageFeature.mean, imageFeature.std) = modelData.normalization;
            imageFeature.aspectMode = modelData.aspectMode;
            // Detect
            var detections = predictor.Predict(imageFeature);
            // Visualize
            visualizer.Render(detections);
        }

        void OnDisable () {
            // Dispose the model
            model?.Dispose();
        }
    }
}