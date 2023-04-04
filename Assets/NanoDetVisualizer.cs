/* 
*   NanoDet
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples.Visualizers {

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using NatML.Vision;
    using VideoKit.UI;

    /// <summary>
    /// </summary>
    [RequireComponent(typeof(VideoKitCameraView))]
    public sealed class NanoDetVisualizer : MonoBehaviour {

        #region --Inspector--
        public NanoDetDetection detectionPrefab;
        #endregion


        #region --Client API--
        /// <summary>
        /// Render a set of object detections.
        /// </summary>
        /// <param name="image">Image which detections are made on.</param>
        /// <param name="detections">Detections to render.</param>
        public void Render (params NanoDetPredictor.Detection[] detections) {
            // Delete current
            foreach (var rect in currentRects)
                GameObject.Destroy(rect.gameObject);
            currentRects.Clear();
            // Render rects
            var imageRect = new Rect(0, 0, rawImage.texture.width, rawImage.texture.height);
            foreach (var detection in detections) {
                var rect = Instantiate(detectionPrefab, transform);
                rect.gameObject.SetActive(true);
                rect.Render(rawImage, detection.rect, detection.label, detection.score);
                currentRects.Add(rect);
            }
        }
        #endregion


        #region --Operations--
        private RawImage rawImage;
        private readonly List<NanoDetDetection> currentRects = new List<NanoDetDetection>();

        void Awake () => rawImage = GetComponent<RawImage>();
        #endregion
    }
}