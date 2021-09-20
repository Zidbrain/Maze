using System.Collections.Generic;
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
        private readonly MaskShaderState _ssaoMask;
        private readonly RenderTarget2D _shadowMaps;

        private readonly RenderTarget2D[] _ambientMap;
        private readonly SSAOShaderState _ambientOcclusion;
        private readonly BlurShaderState _blur;

        private readonly Tile _box;

        public Level Level { get; }

        public LightCollection Lights { get; } = new LightCollection();

        public const int LightBatchCount = 5;

        public Color AmbientColor
        {
            get => _ssaoMask.MaskColor;
            set => _ssaoMask.MaskColor = value;
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

            _ambientMap = new RenderTarget2D[2]
            {
                new RenderTarget2D(Instance.GraphicsDevice, 1920, 1080, true, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.DiscardContents),
                new RenderTarget2D(Instance.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.DiscardContents)
            };
            _ambientOcclusion = new SSAOShaderState(Instance.RenderTargets.Depth, Instance.RenderTargets.Normal, Instance.RenderTargets.Position);
            _blur = new BlurShaderState(_ambientMap[0]);

            _ssaoMask = new (Instance.RenderTargets.Color) { Mask = _ambientMap[1] };

            AddLightType(new PointLightShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position));
            AddLightType(new SpotLightShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position));

            _box = new Tile(level, 0.1f, Direction.None)
            {
                LightEnabled = false,
                DrawToMesh = false,
            };

            Level = level;
        }

        private static readonly SamplerState s_shadowSampler = new()
        {
            Filter = TextureFilter.Linear,
            ComparisonFunction = CompareFunction.Less,
            FilterMode = TextureFilterMode.Comparison,
            AddressU = TextureAddressMode.Border,
            AddressV = TextureAddressMode.Border,
            AddressW = TextureAddressMode.Border,
            BorderColor = Color.Red,
            MaxAnisotropy = 4
        };

        private static readonly BlendState s_blendWithbaked = new()
        {
            ColorBlendFunction = BlendFunction.Min,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorWriteChannels = ColorWriteChannels.Red
        };

        private static readonly BlendState s_blendLight = new()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.One
        };

        private void DrawLightSpecific(List<Light> lights, ILightShaderState<Light> shaderState, ShadowMapShaderState[] shadowMapShaderStates, int offset)
        {
            shaderState.CameraPosition = Instance.Level.Player.Position;

            Instance.GraphicsDevice.SamplerStates[1] = s_shadowSampler;

            for (var i = 0; i <= lights.Count / LightBatchCount; i++)
            {
                shaderState.LightingData.Fill(null);

                Instance.GraphicsDevice.BlendState = BlendState.Opaque;

                for (var j = i * LightBatchCount; j < (i + 1) * LightBatchCount && j < lights.Count; j++)
                {
                    var data = lights[j];
                    shaderState.LightingData[j - i * LightBatchCount] = data;

                    if (lights[j].ShadowsEnabled)
                    {
                        Instance.GraphicsDevice.SetRenderTarget(_shadowMaps, j - i * LightBatchCount);

                        Instance.DrawQuad(shadowMapShaderStates[offset + j], false);
                    }
                }

                if (lights.Count != 0 || i == 0 && offset == 0)
                {
                    Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);

                    if (i == 0 && offset == 0)
                    {
                        Instance.DrawQuad(_ssaoMask, false);
                    }

                    Instance.GraphicsDevice.BlendState = s_blendLight;
                    Instance.DrawQuad(shaderState, false);
                }
            }
        }

        public void Draw()
        {
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Instance.GraphicsDevice.SetRenderTarget(_ambientMap[0]);

            Instance.DrawQuad(_ambientOcclusion, false);

            Instance.GraphicsDevice.SetRenderTarget(_ambientMap[1]);
            Instance.DrawQuad(_blur, false);

            //Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            _box.Position = Instance.Level.Player.Position;
            Instance.Level.Objects.Add(_box);

            var removed = new Stack<Light>();
            foreach (var light in Lights)
            {
                if (!Instance.Frustum.Intersects(new BoundingSphere(light.Position, light.Radius)))
                    removed.Push(light);
            }
            foreach (var light in removed)
                Lights.Remove(light);

            var shaderstates = new ShadowMapShaderState[Lights.Count];

            var i = 0;
            var dynamicObjects = Level.Objects.Static(false).Evaluate();

            var prev = Instance.GraphicsDevice.BlendState;
            Instance.GraphicsDevice.BlendState = s_blendWithbaked;

            foreach (var light in Lights)
            {
                if (light.ShadowsEnabled)
                    if (light.IsStatic)
                        shaderstates[i] = light.GetShadows(dynamicObjects);
                    else
                        shaderstates[i] = light.GetShadows(Level.Objects);
                i++;
            }

            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Instance.GraphicsDevice.BlendState = prev;

            i = 0;
            foreach (var (lights, shaderState) in Lights.GetAllTypesData())
            {
                DrawLightSpecific(lights, shaderState, shaderstates, i);
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
