using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SparkParticle
{
    public Vector3 Position;
    public Vector3 PositionBack;
    public Vector3 LastPosition;

    public Vector3 Velocity;
    public Vector3 Up;
    public Vector3 Left;

    public float Speed;
    public float Life;
    public float PositionCheck;

    public int [] Indices;

    public int UVType;
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SparkController : MonoSingleton<SparkController>
{
    // Variable to hold te UV information of the all avalible particles.
    private Vector4[] _sparkTextures;

    // The max number of particles this object can generate at the same time.
    [SerializeField]
    private int _particleCount = 256;

    // The count of texture types a particle can have.
    private int _textureCount = 4;

    // How many texture types are per row.
    [SerializeField]
    private int _textureRow = 1;

    // How many texture types are pern Col
    [SerializeField]
    private int _textureCol = 4;

    // Renderer Data
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    // List of particles.
    private SparkParticle[] _particles;

    // List of dead and live particles.
    private List<SparkParticle>  _liveParticles = new List<SparkParticle>();
    private Queue<SparkParticle> _deadParticles = new Queue<SparkParticle>();

    // Vertex data
    private Vector3[]   _vertices;
    private int[]       _triIndices;
    private Vector2[]   _texUVs;
    private Color[]     _colors;

    // Flags to know if something need to be updated.
    private bool _needVertexUpdate  = false;
    private bool _needUVUpdate      = false;
    private bool _needColorUpdate   = false;

    protected override void OnStart()
    {
        // How many types of texture the particle will have
        _textureCount = _textureRow * _textureCol;

        _sparkTextures = new Vector4[_textureCount];

        // The UV factor to generate the uv of each particle type.
        float factorX = 1 / (float)_textureCol;
        float factorY = 1 / (float)_textureRow;

        for (int a = 0;a < _textureCount; a++)
        {
            int cellX = a % _textureCol;
            int cellY = a / _textureCol;
            // Store the uv information of each particle type
            _sparkTextures[a] = new Vector4(cellX * factorX, cellX * factorX + factorX, cellY* factorY, cellY * factorY + factorY);
        }

        _meshFilter   = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        // Generate the vertex data objects. 
        _vertices       = new Vector3[4 * _particleCount]; // We need 4 vertex per particles as will be rendered as quads.
        _texUVs         = new Vector2[4 * _particleCount]; // We also need 4 uv coords per particle to match the quantity of vertices.
        _colors         = new Color[4 * _particleCount];   // Also each particle can have unique color so all vertex need to have a color.
        _triIndices     = new int[6 * _particleCount];     // We need 6 indices per quad to generate the 2 triangles.

        _particles = new SparkParticle[_particleCount];

        for (int a = 0;a < _particles.Length;++a)
        {
            _particles[a] = new SparkParticle();
            _particles[a].UVType = -1;
            _deadParticles.Enqueue(_particles[a]);
        }

        _mesh = new Mesh();
        _meshFilter.sharedMesh = _mesh;

        GenerateInitialCellParticleMesh();
    }

    // Function to generate all the initial data of the particles. 
    public void GenerateInitialCellParticleMesh()
    {
        for (int a = 0; a < _particles.Length; ++a)
        {
            SparkParticle particle = _particles[a];

            _texUVs[a * 4 + 0] = new Vector2(0, 1);
            _texUVs[a * 4 + 1] = new Vector2(0, 0);
            _texUVs[a * 4 + 2] = new Vector2(1, 0);
            _texUVs[a * 4 + 3] = new Vector2(1, 1);

            // For each particle we store what vertices use to update them later.
            particle.Indices = new int[4];
            particle.Indices[0] = a * 4 + 0;
            particle.Indices[1] = a * 4 + 1;
            particle.Indices[2] = a * 4 + 2;
            particle.Indices[3] = a * 4 + 3;

            // All vertices are set to zero. If you form a quad or triangle in the same point is called a "degenerated" triagle. 
            // Will not be rendered
            _vertices[a * 4 + 0] = new Vector3(0, 0, 0);
            _vertices[a * 4 + 1] = new Vector3(0, 0, 0);
            _vertices[a * 4 + 2] = new Vector3(0, 0, 0);
            _vertices[a * 4 + 3] = new Vector3(0, 0, 0);

            // All color are set to white to not affect the final color by default.
            _colors[a * 4 + 0] = Color.white;
            _colors[a * 4 + 1] = Color.white;
            _colors[a * 4 + 2] = Color.white;
            _colors[a * 4 + 3] = Color.white;

            // The indices dont change in the lifetime of this object so are defined just one time.
            // 6 indices are needed to create 2 triangles. In the case of unity the triangle is clockwise.
            _triIndices[a * 6 + 0] = a * 4 + 1;
            _triIndices[a * 6 + 1] = a * 4 + 0;
            _triIndices[a * 6 + 2] = a * 4 + 3;

            _triIndices[a * 6 + 3] = a * 4 + 1;
            _triIndices[a * 6 + 4] = a * 4 + 3;
            _triIndices[a * 6 + 5] = a * 4 + 2;
        }
        
        // set the arrays to the final mesh.
        _mesh.vertices = _vertices;
        _mesh.uv = _texUVs;
        _mesh.colors = _colors;
        _mesh.triangles = _triIndices;

    }

    public void SpawnSpark(Vector3 position, Color color, int uvType = -1)
    {
        if(_deadParticles.Count > 0)
        {
            if(uvType == -1)
            {
                uvType = Random.Range(0, _textureCount);
            }

            SparkParticle particle = _deadParticles.Dequeue();
            particle.Position = position;
            particle.LastPosition = position;
            particle.PositionBack = position;
           
            particle.Velocity = new Vector3(Random.Range(-18,18), Random.Range(-18, 18), 0);
            particle.Up = particle.Velocity.normalized;
            float size = 0.05f;
            particle.Left = new Vector3(-particle.Up.y, particle.Up.x) * size;
            particle.Life = 0.7f;
           
            particle.PositionCheck = 0.05f;

            // Set UV: only set the uv if the new particle has changed the previous uv.
            if (particle.UVType != uvType)
            {
                Vector4 uv = _sparkTextures[(int)uvType];

                _texUVs[particle.Indices[0]] = new Vector2(uv.x, uv.w);
                _texUVs[particle.Indices[1]] = new Vector2(uv.x, uv.z);
                _texUVs[particle.Indices[2]] = new Vector2(uv.y, uv.z);
                _texUVs[particle.Indices[3]] = new Vector2(uv.y, uv.w);

                _needUVUpdate = true;
                particle.UVType = uvType;
            }
            // Set Color
            _colors[particle.Indices[0]] = color;
            _colors[particle.Indices[1]] = color;
            _colors[particle.Indices[2]] = color;
            _colors[particle.Indices[3]] = color;
            _needColorUpdate = true;

            _liveParticles.Add(particle);
        }
        else
        {
            Debug.LogWarning("Spark Controller Pushing The Limit! Needed more than "+ _particleCount+" particles");
        }
    }

    public void SpawnMultipleSparks(Vector3 position,int count, Color color,int texture = -1)
    {
        for(int a = 0;a < count;a++)
        {
            if (_deadParticles.Count > 0)
            {
                SpawnSpark(position, color, texture);
            }
            else
            {
                break;
            }
        }
    }
    public void Update()
    {
        for (int a = 0; a < _liveParticles.Count; ++a)
        {
            SparkParticle particle = _liveParticles[a];

            particle.Position += particle.Velocity * Time.deltaTime;

            particle.Velocity += -particle.Velocity * 5.0f * Time.deltaTime;

            // update the 4 vertex of each particle.
            _vertices[particle.Indices[0]] = new Vector3(particle.Position.x     + particle.Left.x  , particle.Position.y     + particle.Left.y, particle.Position.z);
            _vertices[particle.Indices[1]] = new Vector3(particle.LastPosition.x + particle.Left.x  , particle.LastPosition.y + particle.Left.y, particle.Position.z);
            _vertices[particle.Indices[2]] = new Vector3(particle.LastPosition.x - particle.Left.x  , particle.LastPosition.y - particle.Left.y, particle.Position.z);
            _vertices[particle.Indices[3]] = new Vector3(particle.Position.x     - particle.Left.x  , particle.Position.y     - particle.Left.y, particle.Position.z);

            particle.Life -= Time.deltaTime;

            // if the particle is already dead transform it into a degenerated quad.
            if(particle.Life < 0)
            {
                _vertices[particle.Indices[0]] = Vector3.zero;
                _vertices[particle.Indices[1]] = Vector3.zero;
                _vertices[particle.Indices[2]] = Vector3.zero;
                _vertices[particle.Indices[3]] = Vector3.zero;

                _liveParticles.RemoveAt(a);
                _deadParticles.Enqueue(particle);
                a--;
            }

            particle.PositionCheck -= Time.deltaTime;
            if (particle.PositionCheck < 0)
            {
                particle.PositionBack = particle.Position;
                particle.PositionCheck = 0.05f;
            }

            particle.LastPosition = Vector3.Lerp(particle.LastPosition, particle.PositionBack, Time.deltaTime * 10.0f);


            _needVertexUpdate = true;
        }

        // Update mesh arrays if something change and is needed.
        if(_needColorUpdate)
        {
            _mesh.colors = _colors;
            _needColorUpdate = false;
        }

        if(_needUVUpdate)
        {
            _mesh.uv = _texUVs;
            _needUVUpdate = false;
        }

        if (_needVertexUpdate)
        {
            _mesh.vertices = _vertices;
            // recalculate bounds to avoid culling in some cases. 
            // this function is expensive. The bound can be set as a big box to avoid culling at all.
            _mesh.RecalculateBounds(); 
            _needVertexUpdate = false;
        }
    }

}

