﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics.Shaders;
using static Maze.Maze;
using Maze.Engine;
using System;
using System.Collections;

namespace Maze.Graphics
{
    public class LightCollection : ICollection<Light>
    {
        private int _count;
        private readonly Dictionary<Type, (List<Light> lights, ILightShaderState<Light> shaderState)> _typedLists = new();

        public int Count => _count;

        public bool IsReadOnly { get; } = false;

        public void AddLightType<TLight>(ILightShaderState<TLight> shaderState) where TLight : Light
        {
            if (!_typedLists.ContainsKey(typeof(TLight)))
                _typedLists.Add(typeof(TLight), (new List<Light>(LightEngine.LightBatchCount), shaderState));
        }
        public void ChangeShaderState<TLight>(ILightShaderState<TLight> shaderState) where TLight : Light
        {
            var tuple = _typedLists[typeof(TLight)];
            _typedLists[typeof(TLight)] = (tuple.lights, shaderState);
        }

        public IEnumerable<(List<Light> lights, ILightShaderState<Light> shaderState)> GetAllTypesData() =>
            _typedLists.Values;

        public void Add(Light item)
        {
            try
            {
                _typedLists[item.GetType()].lights.Add(item);
                _count++;
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"This collection cannot store lights of this type: {item.GetType()}. Use {typeof(LightCollection).GetMethod("AddLightType")} first.");
            }
        }

        public void Clear()
        {
            _count = 0;

            foreach (var (lights, _) in _typedLists.Values)
                lights.Clear();
        }

        public bool Contains(Light item)
        {
            try
            {
                return _typedLists[item.GetType()].lights.Contains(item);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public void CopyTo(Light[] array, int arrayIndex)
        {
            var offset = 0;
            foreach (var (lights, _) in _typedLists.Values)
            {
                lights.CopyTo(array, arrayIndex + offset);
                offset += lights.Count;
            }
        }

        public struct Enumerator : IEnumerator<Light>
        {
            private Dictionary<Type, (List<Light> lights, ILightShaderState<Light>)>.ValueCollection.Enumerator _enumerator;
            private List<Light>.Enumerator _listEnumerator;

            public Enumerator(LightCollection collection)
            {
                _enumerator = collection._typedLists.Values.GetEnumerator();
                if (!_enumerator.MoveNext())
                    _listEnumerator = new List<Light>.Enumerator();
                _listEnumerator = _enumerator.Current.lights.GetEnumerator();
            }

            public Light Current => _listEnumerator.Current;

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                if (!_listEnumerator.MoveNext())
                {
                    if (!_enumerator.MoveNext())
                        return false;
                    else
                    {
                        _listEnumerator = _enumerator.Current.lights.GetEnumerator();
                        return MoveNext();
                    }
                }

                return true;
            }

            public void Reset()
            {
                (_enumerator as IEnumerator).Reset();
                if (!_enumerator.MoveNext())
                    _listEnumerator = new List<Light>.Enumerator();
                _listEnumerator = _enumerator.Current.lights.GetEnumerator();
            }
        }

        public IEnumerator<Light> GetEnumerator() =>
            new Enumerator(this);

        public bool Remove(Light item)
        {
            try
            {
                if (_typedLists[item.GetType()].lights.Remove(item))
                {
                    _count--;
                    return true;
                }
                return false;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class LightEngine : IDrawable, IDisposable
    {
        private readonly GammaShaderState _gamma;
        private readonly ShadowMapShaderState _shadowState;
        private readonly RenderTarget2D _shadowMaps;

        private readonly RenderTarget2D _ambientMap;
        private readonly SSAOShaderState _ambientOcclusion;
        private readonly BlurShaderState _blur;

        private readonly Tile _box;

        public Level Level { get; }

        public LightCollection Lights { get; } = new LightCollection();

        public const int LightBatchCount = 5;

        public Color AmbientColor
        {
            get => _gamma.MaskColor;
            set => _gamma.MaskColor = value;
        }

        public void AddLightType<TLight>(ILightShaderState<TLight> shaderState) where TLight : Light
        {
            shaderState.ShadowMaps = _shadowMaps;
            Lights.AddLightType(shaderState);
        }

        public void ChangeShaderSate<TLight>(ILightShaderState<TLight> shaderState) where TLight : Light =>
            Lights.ChangeShaderState(shaderState);

        public LightEngine(Level level)
        {
            _shadowMaps = new RenderTarget2D(Instance.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents, false, LightBatchCount);

            _ambientMap = new RenderTarget2D(Instance.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Single, DepthFormat.None, 2, RenderTargetUsage.PreserveContents);
            _ambientOcclusion = new SSAOShaderState(Instance.RenderTargets.Depth, Instance.RenderTargets.Normal, Instance.RenderTargets.Position);
            _blur = new BlurShaderState(_ambientMap);

            _gamma = new GammaShaderState(Instance.RenderTargets.Color) { Mask = _ambientMap };

            AddLightType(new PointLightShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position));
            AddLightType(new SpotLightShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position));

            _shadowState = new ShadowMapShaderState(Instance.RenderTargets.Position);

            _box = new Tile(level, 0.1f, Direction.None)
            {
                LightEnabled = false,
                DrawToMesh = false,
            };

            Level = level;
        }

        private void DrawLightSpecific(List<Light> lights, ILightShaderState<Light> shaderState, Texture2D[] depthMaps, Matrix[][] lightViewMatrices, int start)
        {
            shaderState.CameraPosition = Instance.Level.CameraPosition;

            for (var i = 0; i <= lights.Count / LightBatchCount; i++)
            {
                shaderState.LightingData.Fill(null);
                for (var j = i * LightBatchCount; j < (i + 1) * LightBatchCount && j < lights.Count; j++)
                {
                    var data = lights[j];
                    shaderState.LightingData[j - i * LightBatchCount] = data;

                    if (lights[j].ShadowsEnabled)
                    {
                        _shadowState.LightPosition = data.Position;
                        _shadowState.DepthMap = depthMaps[start + j];
                        _shadowState.LightViewMatrices = lightViewMatrices[start + j];

                        Instance.GraphicsDevice.SetRenderTarget(_shadowMaps, j - i * LightBatchCount);

                        Instance.DrawQuad(_shadowState);
                    }
                }

                Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
                Instance.DrawQuad(shaderState);
            }
        }

        public void Draw()
        {
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Instance.GraphicsDevice.SetRenderTarget(_ambientMap);

            Instance.DrawQuad(_ambientOcclusion);
            Instance.Present();

            _blur.IsVertical = false;
            Instance.DrawQuad(_blur);
            Instance.Present();
            _blur.IsVertical = true;
            Instance.DrawQuad(_blur);

            Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
            Instance.GraphicsDevice.Clear(Color.Transparent);
            Instance.DrawQuad(_gamma);
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            _box.Position = Instance.Level.CameraPosition;
            Instance.Level.Objects.Add(_box);

            var removed = new Stack<Light>();
            foreach (var light in Lights)
            {
                if (!Instance.Frustum.Intersects(new BoundingSphere(light.Position, light.Radius)))
                    removed.Push(light);
            }
            foreach (var light in removed)
                Lights.Remove(light);

            var depthMaps = new Texture2D[Lights.Count];
            var matrices = new Matrix[Lights.Count][];

            var i = 0;
            var dynamicObjects = Level.Objects.Static(false).Evaluate();
            foreach (var light in Lights)
            {
                if (light.ShadowsEnabled)
                    if (light.IsStatic)
                        depthMaps[i] = light.GetShadows(dynamicObjects, out matrices[i]);
                    else
                        depthMaps[i] = light.GetShadows(Level.Objects, out matrices[i]);
                i++;
            }

            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            _shadowState.Position = Instance.RenderTargets.Position;

            i = 0;
            foreach (var (lights, shaderState) in Lights.GetAllTypesData())
            {
                DrawLightSpecific(lights, shaderState, depthMaps, matrices, i);
                i += lights.Count;
            }

            Instance.Level.Objects.Remove(_box);

            foreach (var light in removed)
                Lights.Add(light);
        }

        ~LightEngine() => Dispose();

        public void Dispose()
        {
            _shadowMaps.Dispose();
            _box.Dispose();

            foreach (var light in Lights)
                light.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
