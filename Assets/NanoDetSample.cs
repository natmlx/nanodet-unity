/* 
*   NanoDet
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using UnityEngine;
    using NatML.VideoKit;
    using NatML.Vision;
    using Visualizers;

    public sealed class NanoDetSample : MonoBehaviour {

        [Header(@"VideoKit")]
        public VideoKitCameraManager cameraManager;

        [Header(@"UI")]
        public NanoDetVisualizer visualizer;

        private MLModelData modelData;
        private MLModel model;
        private NanoDetPredictor predictor;

        private async void Start () {
            // Fetch the model data from NatML Hub
            modelData = await MLModelData.FromHub("@natsuite/nanodet");
            // Create the model
            model = new MLEdgeModel(modelData);
            // Create the NanoDet predictor
            predictor = new NanoDetPredictor(model, modelData.labels);
            // Listen for camera frames
            cameraManager.OnFrame.AddListener(OnCameraFrame);
        }

        private void OnCameraFrame (CameraFrame frame) {
            // Create input feature
            var feature = frame.feature;
            (feature.mean, feature.std) = modelData.normalization;
            feature.aspectMode = modelData.aspectMode;
            // Detect
            var detections = predictor.Predict(feature);
            // Visualize
            visualizer.Render(detections);
        }

        void OnDisable () => model?.Dispose(); // Dispose the model
    }
}