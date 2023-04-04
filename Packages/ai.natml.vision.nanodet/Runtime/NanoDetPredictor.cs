/* 
*   NanoDet
*   Copyright © 2023 NatML Inc. All Rights Reserved.
*/

namespace NatML.Vision {

    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Linq;
    using UnityEngine;
    using NatML.Features;
    using NatML.Internal;
    using NatML.Types;

    /// <summary>
    /// NanoDet predictor for general object detection.
    /// This predictor accepts an image feature and produces a list of detections.
    /// Each detection is comprised of a normalized rect, label, and detection score.
    /// </summary>
    public sealed class NanoDetPredictor : IMLPredictor<NanoDetPredictor.Detection[]> {

        #region --Types--
        /// <summary>
        /// Detection.
        /// </summary>
        public struct Detection {
            
            /// <summary>
            /// Normalized detection rect.
            /// </summary>
            public Rect rect;
            
            /// <summary>
            /// Detection label.
            /// </summary>
            public string label;

            /// <summary>
            /// Normalized detection score.
            /// </summary>
            public float score;
        }
        #endregion


        #region --Client API--
        /// <summary>
        /// Predictor tag.
        /// </summary>
        public const string Tag = "@natsuite/nanodet";

        /// <summary>
        /// Detect objects in an image.
        /// </summary>
        /// <param name="inputs">Input image.</param>
        /// <returns>Detected objects.</returns>
        public unsafe Detection[] Predict (params MLFeature[] inputs) {
            // Check
            if (inputs.Length != 1)
                throw new ArgumentException(@"NanoDet predictor expects a single feature", nameof(inputs));
            // Check type
            var input = inputs[0];
            var imageType = MLImageType.FromType(input.type);
            var imageFeature = input as MLImageFeature;
            if (!imageType)
                throw new ArgumentException(@"NanoDet predictor expects an an array or image feature", nameof(inputs));
            // Preprocess
            if (imageFeature != null) {
                (imageFeature.mean, imageFeature.std) = model.normalization;
                imageFeature.aspectMode = model.aspectMode;
            }
            // Predict
            using var inputFeature = (input as IMLEdgeFeature).Create(inputType);
            using var outputFeatures = model.Predict(inputFeature);
            // Marshal
            var logits8 = new MLArrayFeature<float>(outputFeatures[0]);             // (1,1600,80)
            var logits16 = new MLArrayFeature<float>(outputFeatures[1]);            // (1,400,80)
            var logits32 = new MLArrayFeature<float>(outputFeatures[2]);            // (1,100,80)
            var displacements8 = new MLArrayFeature<float>(outputFeatures[3]);      // (1,1600,32)
            var displacements16 = new MLArrayFeature<float>(outputFeatures[4]);     // (1,400,32)
            var displacements32 = new MLArrayFeature<float>(outputFeatures[5]);     // (1,100,32)
            var softmax = stackalloc float[8];
            var boxEdges = stackalloc float[4];
            var featureWidthInv = 1f / inputType.width;
            var featureHeightInv = 1f / inputType.height;
            candidateBoxes.Clear();
            candidateScores.Clear();
            candidateLabels.Clear();
            foreach (var (stride, logits, displacements, anchors) in new [] {
                (8, logits8, displacements8, anchors8),
                (16, logits16, displacements16, anchors16),
                (32, logits32, displacements32, anchors32),
            }) {
                var boxes = displacements.View(-1, 32); // Squeeze
                for (int i = 0, ilen = anchors.Length, llen = logits.shape[2]; i < ilen; ++i) {
                    // Check
                    var label = 0;
                    for (var l = 1; l < llen; ++l)
                        label = logits[0,i,l] > logits[0,i,label] ? l : label;
                    var score = logits[0,i,label];
                    if (score < minScore)
                        continue;
                    // Decode box offsets
                    for (var s = 0; s < 4; ++s) {
                        Softmax(boxes, i, 8 * s, softmax);
                        var displacement = 0f;
                        for (var d = 0; d < 8; d++)
                            displacement += softmax[d] * d;
                        boxEdges[s] = displacement;
                    }
                    // Create rect
                    var anchor = anchors[i];
                    var x1 = anchor.x - boxEdges[0] * stride;
                    var y1 = anchor.y - boxEdges[1] * stride;
                    var x2 = anchor.x + boxEdges[2] * stride;
                    var y2 = anchor.y + boxEdges[3] * stride;
                    var rawBox = Rect.MinMaxRect(
                        x1 * featureWidthInv,
                        1f - y2 * featureHeightInv,
                        x2 * featureWidthInv,
                        1f - y1 * featureHeightInv
                    );
                    var box = imageFeature?.TransformRect(rawBox, inputType) ?? rawBox;
                    // Add
                    candidateBoxes.Add(box);
                    candidateScores.Add(score);
                    candidateLabels.Add(model.labels[label]);
                }
            }
            var keepIdx = MLImageFeature.NonMaxSuppression(candidateBoxes, candidateScores, maxIoU);
            var result = new List<Detection>();
            foreach (var idx in keepIdx) {
                var detection = new Detection {
                    rect = candidateBoxes[idx],
                    label = candidateLabels[idx],
                    score = candidateScores[idx]
                };
                result.Add(detection);
            }
            // Return
            return result.ToArray();
        }

        /// <summary>
        /// Dispose the model and release resources.
        /// </summary>
        public void Dispose () => model.Dispose();

        /// <summary>
        /// Create the NanoDet predictor.
        /// </summary>
        /// <param name="minScore">Minimum candidate score.</param>
        /// <param name="maxIoU">Maximum intersection-over-union score for overlap removal.</param>
        /// <param name="configuration">Edge model configuration.</param>
        /// <param name="accessKey">NatML access key.</param>
        public static async Task<NanoDetPredictor> Create (
            float minScore = 0.35f,
            float maxIoU = 0.5f,
            MLEdgeModel.Configuration configuration = null,
            string accessKey = null
        ) {
            var model = await MLEdgeModel.Create(Tag, configuration, accessKey);
            var predictor = new NanoDetPredictor(model, minScore, maxIoU);
            return predictor;
        }
        #endregion


        #region --Operations--
        private readonly MLEdgeModel model;
        private readonly float minScore;
        private readonly float maxIoU;
        private readonly MLImageType inputType;
        private readonly Vector2[] anchors8;
        private readonly Vector2[] anchors16;
        private readonly Vector2[] anchors32;
        private readonly List<Rect> candidateBoxes;
        private readonly List<float> candidateScores;
        private readonly List<string> candidateLabels;

        private NanoDetPredictor (MLModel model, float minScore, float maxIoU) {
            this.model = model as MLEdgeModel;
            this.minScore = minScore;
            this.maxIoU = maxIoU;
            this.inputType = model.inputs[0] as MLImageType;
            this.anchors8 = GenerateAnchors(inputType.width, inputType.height, 8);
            this.anchors16 = GenerateAnchors(inputType.width, inputType.height, 16);
            this.anchors32 = GenerateAnchors(inputType.width, inputType.height, 32);
            this.candidateBoxes = new List<Rect>();
            this.candidateScores = new List<float>();
            this.candidateLabels = new List<string>();
        }

        private static Vector2[] GenerateAnchors (int width, int height, int stride) {
            var gridWidth = Mathf.FloorToInt((float)width / stride);
            var gridHeight = Mathf.FloorToInt((float)height / stride);
            var result = new List<Vector2>();
            for (var j = 0; j < gridHeight; ++j)
                for (var i = 0; i < gridWidth; ++i) {
                    var sx = stride * i;
                    var sy = stride * j;
                    var cx = sx + 0.5f * (stride - 1);
                    var cy = sy + 0.5f * (stride - 1);
                    result.Add(new Vector2(cx, cy));
                }
            return result.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Softmax (MLArrayFeature<float> input, int start, int offset, float* dst) {
            var sum = 0f;
            for (var i = 0; i < 8; ++i) {
                var e = Mathf.Exp(input[start, offset + i]);
                dst[i] = e;
                sum += e;
            }
            var sumInv = 1f / sum;
            for (var i = 0; i < 8; ++i)
                dst[i] *= sumInv;
        }
        #endregion
    }
}