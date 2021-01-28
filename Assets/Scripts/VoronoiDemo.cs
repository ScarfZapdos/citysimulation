using UnityEngine;
using System.Collections.Generic;
using static System.Math;
using Delaunay;
using Delaunay.Geo;
using UnityEditor.AI;

public class CoupleVector2 {
	public Vector2 v1;
	public Vector2 v2;

	public CoupleVector2(Vector2 u1,Vector2 u2)
    {
		v1 = u1;
		v2 = u2;
    }

	public bool isEq(Vector2 a1,Vector2 a2)
    {
		return (((Vector2.Distance(this.v1, a1) < 10) &&
				(Vector2.Distance(this.v2, a2) < 10))
			||
				((Vector2.Distance(this.v2, a1) < 10) &&
				(Vector2.Distance(this.v1, a2) < 10)));
	}
}

public class VoronoiDemo : MonoBehaviour
{

    public Material land;
    public const int NPOINTS = 300;
    public const int WIDTH = 2000;
    public const int HEIGHT = 2000;
	public const int PLANE_SCALE = 3;
	public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;
	public GameObject route;
	public GameObject house;
	public GameObject skyscraper1;
	public GameObject skyscraper2;
	public GameObject skyscraper3;
	public GameObject citizen;
	public GameObject city;
	public List<GameObject> skyscrapers = new List<GameObject>();
	public const int CITY_CENTERS = 1;
	public const int CITY_RADIUS = 500;


	private List<Vector2> m_points;
	private List<Vector2> m_voronoi;
	private List<Vector2> m_seenOnce = new List<Vector2>();
	private List<CoupleVector2> m_roadCreated = new List<CoupleVector2>();
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;
	public List<GameObject> officeBuilding = new List<GameObject>();
	private List<GameObject> housesSpawned = new List<GameObject>();
	private List<GameObject> population = new List<GameObject>();

	private bool rebuilt = false;

	int[] centersX = new int[CITY_CENTERS];
	int[] centersY = new int[CITY_CENTERS];
	List<Vector2> rdm_axis = new List<Vector2>();
	int n_rdm = 0;

	private Vector2 closestCenter(Vector2 p)
	{
		float minDist = float.MaxValue;
		Vector2 closest = new Vector2(0f, 0f);
		float d = 0f;
		for (int k = 0; k < CITY_CENTERS; k++)
		{
			Vector2 centerk = new Vector2(centersX[k], centersY[k]);
			d = Vector2.Distance(p, centerk);
			if (d < minDist)
			{
				minDist = d;
				closest = centerk;
			}
		}
		return closest;
	}

	private Vector2 closestPoint(Vector2 prev,Vector2 p)
	{
		float minDist = float.MaxValue;
		Vector2 closest = new Vector2(0f, 0f);
		float d = 0f;
		for (int k = 0; k < rdm_axis.Count; k++)
		{
			d = Vector2.Distance(p, rdm_axis[k]);
			if ((d < minDist) && (Vector2.Distance(prev, rdm_axis[k])>=d))
			{
				minDist = d;
				closest = rdm_axis[k];
			}
		}
		return closest;
	}

	private void assignWorks()
    {
		foreach (GameObject citizenx in population)
		{
			CitizenBehaviour behavex = citizenx.GetComponent<CitizenBehaviour>();
			while(!behavex.office)
				behavex.office = officeBuilding[Random.Range(0, officeBuilding.Count)];
		}
	}

	private void buildNearHousesOneSide(Vector2 v1, Vector2 v2)
    {
		Vector2 pointer = v2 - v1;
		float rnb = Vector2.Distance(v1, v2) / 0.30f; // Maximum number of buildings
		int lenRdm = (int)rnb;
		//List<float> rdmP = new List<float>;
		for (int k = 0; k < lenRdm; k++)
        {
			float randpos = Random.value;
			float rot = Vector2.SignedAngle(Vector2.right, v2 - v1);
			Vector3 posx = new Vector3(v1.y + randpos * pointer.y, 0, v1.x + randpos * pointer.x);
			GameObject housex = Instantiate(house, posx, Quaternion.Euler(0, 90 + rot, 0));
			housesSpawned.Add(housex);
			GameObject citizenx = Instantiate(citizen, posx , Quaternion.Euler(0, 90 + rot, 0));
			CitizenBehaviour cb = citizenx.GetComponentInChildren<CitizenBehaviour>();
			if (cb)
				cb.home = housex;
			population.Add(citizenx);
		}
	}

	private void buildNearHouses(Vector2 v1, Vector2 v2)
	{
		buildNearHousesOneSide(v1, v2);
		buildNearHousesOneSide(v2, v1);
	}

	private void buildSkyscrapersOneSide(Vector2 v1, Vector2 v2)
	{
		Vector2 pointer = v2 - v1;
		float rnb = Vector2.Distance(v1, v2) / 0.30f; // Maximum number of buildings
		int lenRdm = (int)rnb;
		//List<float> rdmP = new List<float>;
		for (int k = 0; k < lenRdm; k++)
		{
			float randpos = Random.value;
			int randBuilding = Random.Range(0, skyscrapers.Count);
			float rot = Vector2.SignedAngle(Vector2.right, v2 - v1);
			GameObject officex = Instantiate(skyscrapers[randBuilding], new Vector3(v1.y + randpos * pointer.y, 0, v1.x + randpos * pointer.x), Quaternion.Euler(0, 90 + rot, 0));
			officeBuilding.Add(officex);
		}
	}

	private void buildSkyscrapers(Vector2 v1, Vector2 v2)
	{
		buildSkyscrapersOneSide(v1, v2);
		buildSkyscrapersOneSide(v2, v1);
	}

	private void buildRoad(Vector2 v1, Vector2 v2)
    {
		float rot = Vector2.SignedAngle(Vector2.right, v2 - v1);
		GameObject routex = Instantiate(route, new Vector3(v1.y, 0, v1.x), Quaternion.Euler(0, 90 + rot, 0));
		routex.transform.localScale = new Vector3(Vector2.Distance(v1, v2), 1, 1);
		buildSkyscrapers(v1, v2);
	}

	private float [,] createMap() 
    {
        float [,] map = new float[WIDTH, HEIGHT];
		for (int k = 0; k < CITY_CENTERS; k++)
		{
			centersX[k] = Random.Range(CITY_RADIUS, WIDTH - CITY_RADIUS + 1);
			centersY[k] = Random.Range(CITY_RADIUS, HEIGHT - CITY_RADIUS + 1);
			map[centersX[k], centersY[k]] = 1.0f;
		}

		for (int i = 0; i < WIDTH; i++)
		{
			for (int j = 0; j < HEIGHT; j++)
			{
				Vector2 point = new Vector2(i, j);
				Vector2 closest = closestCenter(point);
				map[i, j] = Mathf.Max(Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety),
					//(float)PI/2 - (float)Atan(Vector2.Distance(point, closest)-30));
					//(float)(Cos(Vector2.Distance(point, closest)/100) + 1) / 2);
					1f - (Vector2.Distance(point, closest)) / CITY_RADIUS);
				/*if (1f - (Vector2.Distance(point, closest)) / CITY_RADIUS > 0.85)
					map[i, j] = 1;
				else
					map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety);*/
			}
		}



		return map;
    }

	void Start ()
	{
        float [,] map=createMap();
        Color[] pixels = createPixelMap(map);

		skyscrapers.Add(skyscraper1);
		skyscrapers.Add(skyscraper2);
		skyscrapers.Add(skyscraper3);
		/* Create random points points */
		/*m_points = new List<Vector2> ();
		List<uint> colors = new List<uint> ();
		for (int i = 0; i < NPOINTS; i++) {
			colors.Add ((uint)0);
			Vector2 vec = new Vector2(Random.Range(0, WIDTH-1), Random.Range(0, HEIGHT-1)); 
			m_points.Add (vec);
		}*/

		/* Perso fct to create random points */
		m_points = new List<Vector2>();
		m_voronoi = new List<Vector2>();
		List<uint> colors = new List<uint>();
		for (int i = 0; i < NPOINTS; i++)
		{
			float r = Random.value * Random.value;
			int x = (int)Random.Range(0, WIDTH - 1);
			int y = (int)Random.Range(0, HEIGHT - 1);
			int iter = 0;
			while (map[x, y] < 1 - r && iter < 1000)
			{
				x = Random.Range(0, WIDTH - 1);
				y = Random.Range(0, HEIGHT - 1);
				iter++;
			}

			colors.Add((uint)0);
			Vector2 vec = new Vector2(x, y);
			m_points.Add(vec);
			if (map[x,y]<1)
				m_voronoi.Add(vec);
		}

			/* Generate Graphs */
			Delaunay.Voronoi v = new Delaunay.Voronoi (m_voronoi, colors, new Rect (0, 0, WIDTH, HEIGHT));
		m_edges = v.VoronoiDiagram ();
		m_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		m_delaunayTriangulation = v.DelaunayTriangulation ();
		

		/* Shows Voronoi diagram And generates Roads*/
		Color color = Color.blue;
		for (int i = 0; i < m_edges.Count; i++)
		{
			LineSegment seg = m_edges[i];
			Vector2 left = (Vector2)seg.p0;
			Vector2 right = (Vector2)seg.p1;
			//if (!((1f - Vector2.Distance(closestCenter(left), left) / CITY_RADIUS > 0.68) 
			//		&&
			//		(1f - Vector2.Distance(closestCenter(right), right) / CITY_RADIUS > 0.68)))
			//|| (m_seenOnce.Exists(x=> Vector2.Distance(left,x)<10) && m_seenOnce.Exists(x => Vector2.Distance(right, x) < 10)))
			if (Vector2.Distance((closestCenter(left) / WIDTH * 10 - new Vector2(5f, 5f)) * PLANE_SCALE,(left / WIDTH * 10 - new Vector2(5f, 5f)) *PLANE_SCALE) > 7f && 
			Vector2.Distance((closestCenter(right) / WIDTH * 10 - new Vector2(5f, 5f)) * PLANE_SCALE,(right / WIDTH * 10 - new Vector2(5f, 5f)) * PLANE_SCALE) > 7f)
			{
				Vector2 direction = (right - left) / WIDTH * 10;
				float rot = Vector2.SignedAngle(Vector2.right, right - left);
				GameObject routex = Instantiate(route, new Vector3((left.y / WIDTH * 10 - 5) * PLANE_SCALE, 0, (left.x / HEIGHT * 10 - 5) * PLANE_SCALE), Quaternion.Euler(0, 90 + rot, 0));
				/*if (m_seenOnce.Contains(left) && m_seenOnce.Contains(right))
				{
					Renderer routeRenderer = routex.transform.GetChild(0).gameObject.GetComponent<Renderer>();
					routeRenderer.material.SetColor("_Color", Color.red);
				}*/
				routex.transform.localScale = new Vector3(direction.magnitude * PLANE_SCALE, 1, 1);
				CoupleVector2 routeVect = new CoupleVector2(left, right);
				m_roadCreated.Add(routeVect);
				buildNearHouses((left/ WIDTH * 10 - new Vector2(5f,5f)) * PLANE_SCALE, (right/ WIDTH * 10 - new Vector2(5f,5f)) * PLANE_SCALE);
				if (!(m_seenOnce.Contains(left)))
					m_seenOnce.Add(left);
				if (!(m_seenOnce.Contains(right)))
					m_seenOnce.Add(right);
			}
			//DrawLine (pixels,left, right,color);
		}
		for (int i = 0; i < CITY_CENTERS; i++)
		{
			float xi = ((float)centersX[i] / WIDTH * 10 - 5) * PLANE_SCALE;
			float yi = ((float)centersY[i] / WIDTH * 10 - 5) * PLANE_SCALE;
			Vector2 vi = new Vector2(xi, yi);
			Vector2 vrand = Random.insideUnitCircle;
			vrand.Normalize();
			//Building roads from center to outer circle
			Vector2 vsquareout1 = vi + vrand * 4 * 2;
			float xout1 = vsquareout1.x - vi.x;
			float yout1 = vsquareout1.y - vi.y;
			Vector2 vout1 = new Vector2(vi.x + 1.2f * xout1, vi.y + 1.2f * yout1);
			Vector2 vout2 = new Vector2(vi.x - 1.2f * xout1, vi.y - 1.2f * yout1);

			buildRoad(vout1, vout2);

			vout1 = new Vector2(vi.x - 1.2f * xout1, vi.y + 1.2f * yout1);
			vout2 = new Vector2(vi.x + 1.2f * xout1, vi.y - 1.2f * yout1);

			buildRoad(vout1, vout2);

			Vector2 vrand2 = Random.insideUnitCircle;
			vrand2.Normalize();

			//Building roads from center to outer circle
			Vector2 vsquareout3 = vi + vrand2 * 4 * 2;
			float xout3 = vsquareout3.x - vi.x;
			float yout3 = vsquareout3.y - vi.y;
			Vector2 vout3 = new Vector2(vi.x + 1.2f * xout3, vi.y + 1.2f * yout3);
			Vector2 vout4 = new Vector2(vi.x - 1.2f * xout3, vi.y - 1.2f * yout3);

			buildRoad(vout3, vout4);

			vout3 = new Vector2(vi.x - 1.2f * xout3, vi.y + 1.2f * yout3);
			vout4 = new Vector2(vi.x + 1.2f * xout3, vi.y - 1.2f * yout3);

			buildRoad(vout3, vout4);

			for (int l = 1; l <= 8; l++)
			{
				Vector2 vsquare = vi +  vrand * l;
				float xs = vsquare.x - vi.x;
				float ys = vsquare.y - vi.y;
				//Ensure to have at least a point in each quarter
				rdm_axis.Add(new Vector2(vi.x + xs, vi.y + ys));
				rdm_axis.Add(new Vector2(vi.x - xs, vi.y + ys));
				rdm_axis.Add(new Vector2(vi.x + xs, vi.y - ys));
				rdm_axis.Add(new Vector2(vi.x - xs, vi.y - ys));

				Vector2 vsquare2 = vi + vrand2 * l;
				float xs2 = vsquare2.x - vi.x;
				float ys2 = vsquare2.y - vi.y;
				//Ensure to have at least a point in each quarter
				rdm_axis.Add(new Vector2(vi.x + xs2, vi.y + ys2));
				rdm_axis.Add(new Vector2(vi.x - xs2, vi.y + ys2));
				rdm_axis.Add(new Vector2(vi.x + xs2, vi.y - ys2));
				rdm_axis.Add(new Vector2(vi.x - xs2, vi.y - ys2));
				n_rdm = 8 + Random.Range(10, 14);
				for (int j = 0; j < n_rdm; j++)
				{
					rdm_axis.Add(vi + Random.insideUnitCircle.normalized * l);
				}
				Vector2 v1 = rdm_axis[0];
				Vector2 v2 = rdm_axis[0];
				Vector2 old = rdm_axis[0];
				Vector2 initial = rdm_axis[0];
				//GameObject capsule4 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
				//capsule4.transform.position = new Vector3(v1.y, 1, v1.x);
				rdm_axis.Remove(v1);
				int counter = 0;
				while (rdm_axis.Count > 0 && counter<100)
				{
					for (int k = 0; k < rdm_axis.Count; k++)
					v2 = closestPoint(old,v1);
					bool t = rdm_axis.Remove(v2);
					//Road Spawning
					buildRoad(v1, v2);
					old = v1;
					v1 = v2;
					counter++;
				}
				v2 = initial;
				buildRoad(v1, v2);
			}
		}
		/* Shows Delaunay triangulation */
		/*
 		color = Color.red;
		if (m_delaunayTriangulation != null) {
			for (int i = 0; i < m_delaunayTriangulation.Count; i++) {
					LineSegment seg = m_delaunayTriangulation [i];				
					Vector2 left = (Vector2)seg.p0;
					Vector2 right = (Vector2)seg.p1;
					DrawLine (pixels,left, right,color);
			}
		}*/

		/* Shows spanning tree */
		/*
		color = Color.black;
		if (m_spanningTree != null) {
			for (int i = 0; i< m_spanningTree.Count; i++) {
				LineSegment seg = m_spanningTree [i];				
				Vector2 left = (Vector2)seg.p0;
				Vector2 right = (Vector2)seg.p1;
				DrawLine (pixels,left, right,color);
			}
		}*/

		/* Apply pixels to texture */
		tx = new Texture2D(WIDTH, HEIGHT);
        land.SetTexture ("_MainTex", tx);
		tx.SetPixels (pixels);
		tx.Apply ();
		NavMeshBuilder.BuildNavMesh();
	    assignWorks();
		foreach (GameObject p in population)
        {
			p.GetComponent<CitizenBehaviour>().initGoToWork();
        }
	}

	//void Update()
	//{
	//	if (Time.time > 15 && !rebuilt)
	//	{
	//		NavMeshBuilder.ClearAllNavMeshes();
	//		NavMeshBuilder.BuildNavMesh();
	//		rebuilt = true;
	//	}
	//}

    /* Functions to create and draw on a pixel array */
    private Color[] createPixelMap(float[,] map)
    {
        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
            {
                pixels[i * HEIGHT + j] = Color.Lerp(Color.blue, Color.yellow, map[i, j]);
            }
        return pixels;
    }
    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x<WIDTH&&p.x>=0&&p.y<HEIGHT&&p.y>=0) 
		    pixels[(int)p.x*HEIGHT+(int)p.y]=c;
	}
	// Bresenham line algorithm
	private void DrawLine(Color [] pixels, Vector2 p0, Vector2 p1, Color c) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;

		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		while (true) {
            if (x0>=0&&x0<WIDTH&&y0>=0&&y0<HEIGHT)
    			pixels[x0*HEIGHT+y0]=c;

			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
	}
}