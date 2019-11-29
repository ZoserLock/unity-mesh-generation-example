using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using GameModules;

public class WaterParticle
{
    public Vector3 Normal;
    public float Position;
    public float Velocity;
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterController : MonoSingleton<WaterController>
{
    [SerializeField]
    private Camera _camera;

    // The max number of segment a water planet will have.
    [SerializeField]
    [Range(3,360)]
    private int _segmentCount = 32;

    [SerializeField]
    private float _earthSize = 5;

    [SerializeField]
    private float _waterSize = 5;

    [SerializeField]
    private float _foamSize = 0.25f;

    [Header("Waves")]
    [SerializeField]
    private float _spread = 0.02f;

    [SerializeField]
    private float _dampering = 0.03f;

    [SerializeField]
    private float _waveForce = 0.025f;

    [SerializeField]
    private float _clickForce = -0.85f;

    // Renderer Mesh Data
    private MeshFilter   _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh         _mesh;
    // list of WaterParticles
    private List<WaterParticle> _waterParticles = new List<WaterParticle>();

    // Vertex data
    private Vector3[] _vertices;
    private int[]     _triIndices;
    private Vector2[] _texUVs;
    private Color[] _colors;

    private float[] _rightDeltas;
    private float[] _leftDeltas;


    private bool _started = false;


    float _splashTime = 0;

    protected override void OnStart()
    {
        InputManager.Instance.OnTapEvent += OnTapEvent;

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh();
        _meshFilter.sharedMesh = _mesh;
        _started = true;

        GenerateMesh();
    }

    private void OnTapEvent(Vector3 position)
    {
        var worldPostion = _camera.ScreenToWorldPoint(position);
        worldPostion.z = 0;

        int particleCount = _waterParticles.Count;
        float distance = 9999999.0f;
        WaterParticle selected = null;

        for (int a = 0; a < _waterParticles.Count; ++a)
        {
            var pos = _waterParticles[a].Normal * _waterParticles[a].Position;
            pos.z = 0;
            float dist = Vector3.Distance(pos, worldPostion);

            if(dist< distance)
            {
                selected = _waterParticles[a];
                distance = dist;
            }
        }

        if(selected != null)
        {
            selected.Velocity = _clickForce;
        }

    }

    public void OnValidate()
    {
        GenerateMesh();
    }


    public void GenerateMesh()
    {
        if (!_started || _segmentCount < 3)
        {
            return;
        }

        _mesh.Clear();

        Debug.Log("GenerateMesh");

        int vertexPerSegment = 4;
        int triPerSegment = 5; // 2 Foam Triangles, 2 Water triangle, 1 earth triangle

        // Generate the vertex data objects. 
        _vertices = new Vector3[vertexPerSegment * _segmentCount];
        _texUVs = new Vector2[vertexPerSegment * _segmentCount];
        _colors = new Color[vertexPerSegment * _segmentCount];
        _triIndices = new int[3 * triPerSegment * _segmentCount];
        _waterParticles.Clear();

        _rightDeltas = new float[_segmentCount];
        _leftDeltas  = new float[_segmentCount];



        GenerateInitialParticleData();
        GenerateInitialMeshData();
        UpdateMeshData();

    }

    public void GenerateInitialParticleData()
    {
        float anglePerSegment = 3.14165f * 2.0f / (float)_segmentCount;

        for (int a = 0; a < _segmentCount; ++a)
        {
            WaterParticle particle = new WaterParticle();
            _waterParticles.Add(particle);

            particle.Normal = new Vector3(Mathf.Sin(anglePerSegment * a), Mathf.Cos(anglePerSegment * a),0);
            particle.Position = _waterSize;
            particle.Velocity = 0.0f;
        }

    }

    public void GenerateInitialMeshData()
    {
        int particleCount = _waterParticles.Count;

        for (int a = 0; a < particleCount; ++a)
        {
            int currentIndex = a;
            int nextIndex = (a + 1) % particleCount;

            var particle = _waterParticles[currentIndex];
            var particleNext = _waterParticles[nextIndex];
            float side = ((a % 2) == 0) ? 0.0f : 1.0f;

            _texUVs[currentIndex * 4 + 0] = new Vector2(side, 1.0f);
            _texUVs[currentIndex * 4 + 1] = new Vector2(side, 0.9f);
            _texUVs[currentIndex * 4 + 2] = new Vector2(side, 0.5f);
            _texUVs[currentIndex * 4 + 3] = new Vector2(side, 0.0f);

            _vertices[currentIndex * 4 + 0] = particle.Normal * _foamSize;
            _vertices[currentIndex * 4 + 1] = particle.Normal * _waterSize;
            _vertices[currentIndex * 4 + 2] = particle.Normal * _earthSize;
            _vertices[currentIndex * 4 + 3] = new Vector3(0, 0, 0);

            _colors[currentIndex * 4 + 0] = Color.white;
            _colors[currentIndex * 4 + 1] = Color.white;
            _colors[currentIndex * 4 + 2] = Color.white;
            _colors[currentIndex * 4 + 3] = Color.white;

            //Create triangles for each segment minus the last.
            if (a < particleCount)
            {
                // Foam
                _triIndices[currentIndex * 15 + 0] = currentIndex * 4 + 0;
                _triIndices[currentIndex * 15 + 1] = nextIndex * 4 + 0;
                _triIndices[currentIndex * 15 + 2] = nextIndex * 4 + 1;

                _triIndices[currentIndex * 15 + 3] = currentIndex * 4 + 0;
                _triIndices[currentIndex * 15 + 4] = nextIndex * 4 + 1;
                _triIndices[currentIndex * 15 + 5] = currentIndex * 4 + 1;

                // Water
                _triIndices[currentIndex * 15 + 6] = currentIndex * 4 + 1;
                _triIndices[currentIndex * 15 + 7] = nextIndex * 4 + 1;
                _triIndices[currentIndex * 15 + 8] = nextIndex * 4 + 2;

                _triIndices[currentIndex * 15 + 9] = currentIndex * 4 + 1;
                _triIndices[currentIndex * 15 + 10] = nextIndex * 4 + 2;
                _triIndices[currentIndex * 15 + 11] = currentIndex * 4 + 2;

                // Earth
                _triIndices[currentIndex * 15 + 12] = currentIndex * 4 + 2;
                _triIndices[currentIndex * 15 + 13] = nextIndex * 4 + 2;
                _triIndices[currentIndex * 15 + 14] = currentIndex * 4 + 3;
            }
        }

        // Set the arrays to the final mesh.
        _mesh.vertices = _vertices;
        _mesh.uv = _texUVs;
        _mesh.colors = _colors;
        _mesh.triangles = _triIndices;
    }

    public void UpdateParticleData()
    {
        // Spring update
        int particleCount = _waterParticles.Count;
        for (int a = 0; a < particleCount; ++a)
        {
            var particle = _waterParticles[a];

            float delta = particle.Position - _waterSize;
            float acceleration = -_waveForce * delta - particle.Velocity * _dampering;

            particle.Position += particle.Velocity;
            particle.Velocity += acceleration;
        }

        // Waves
        for (int j = 0; j < 10; j++)
        {
            for (int i = 1; i < particleCount - 1; i++)
            {
                _rightDeltas[i] = _spread * (_waterParticles[i].Position - _waterParticles[i + 1].Position);
                _waterParticles[i + 1].Velocity += _rightDeltas[i];

                _leftDeltas[i] = _spread * (_waterParticles[i].Position - _waterParticles[i - 1].Position);
                _waterParticles[i - 1].Velocity += _leftDeltas[i];
            }

            int end = particleCount - 1;

            _rightDeltas[0] = _spread * (_waterParticles[0].Position - _waterParticles[1].Position);
            _waterParticles[0 + 1].Velocity += _rightDeltas[0];

            _leftDeltas[0] = _spread * (_waterParticles[0].Position - _waterParticles[end].Position);
            _waterParticles[end].Velocity += _leftDeltas[0];

            _rightDeltas[end] = _spread * (_waterParticles[end].Position - _waterParticles[0].Position);
            _waterParticles[0].Velocity += _rightDeltas[end];

            _leftDeltas[end] = _spread * (_waterParticles[end].Position - _waterParticles[end - 1].Position);
            _waterParticles[end - 1].Velocity += _leftDeltas[end];

            for (int i = 1; i < particleCount - 1; i++)
            {
                _waterParticles[i - 1].Position += (_leftDeltas[i]);
                _waterParticles[i + 1].Position += (_rightDeltas[i]);
            }

            _waterParticles[particleCount - 1].Position += (_leftDeltas[0]);
            _waterParticles[1].Position += (_rightDeltas[0]);

            _waterParticles[particleCount - 2].Position += (_leftDeltas[particleCount - 1]);
            _waterParticles[0].Position += (_rightDeltas[particleCount - 1]);
        }
    }

    public void UpdateMeshData()
    {
        int particleCount = _waterParticles.Count;

        for (int a = 0; a < particleCount; ++a)
        {
            int currentIndex = a;
            int nextIndex = (a + 1) % particleCount;

            var particle     = _waterParticles[currentIndex];
            var particleNext = _waterParticles[nextIndex];
            float side = ((a % 2) == 0) ? 0.0f:1.0f;

            _texUVs[currentIndex * 4 + 0] = new Vector2(side, 1.0f);
            _texUVs[currentIndex * 4 + 1] = new Vector2(side, 0.9f);
            _texUVs[currentIndex * 4 + 2] = new Vector2(side, 0.5f);
            _texUVs[currentIndex * 4 + 3] = new Vector2(side, 0.0f);

            _vertices[currentIndex * 4 + 0] = particle.Normal * (particle.Position + _foamSize);
            _vertices[currentIndex * 4 + 1] = particle.Normal * particle.Position;
            _vertices[currentIndex * 4 + 2] = particle.Normal * (particle.Position - _earthSize);
            _vertices[currentIndex * 4 + 3] = new Vector3(0, 0, 0);

            _colors[currentIndex * 4 + 0]   = Color.white;
            _colors[currentIndex * 4 + 1]   = Color.white;
            _colors[currentIndex * 4 + 2]   = Color.white;
            _colors[currentIndex * 4 + 3]   = Color.white;

         
        }
       
        _mesh.vertices = _vertices;
        _mesh.uv = _texUVs;
        _mesh.colors = _colors;
    }

    public void Update()
    {
        UpdateParticleData();
        UpdateMeshData();
    }
}

