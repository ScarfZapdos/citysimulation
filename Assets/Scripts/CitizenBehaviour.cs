using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CitizenBehaviour : MonoBehaviour
{
    public GameObject office;
    private Vector3 office_access;
    private NavMeshPath path;
    NavMeshAgent agent;
    Renderer objRenderer;
    CapsuleCollider coll;
    public GameObject home;
    private Vector3 home_access;
    public bool unemployed = false;
    private bool colored = false;
    public bool at_work = false;
    public bool at_home = false;
    public bool going_to_work = false;
    public bool going_home = false;
    public GameObject lightControl;
    private DayNightController dncontrol;
    private bool is_init = false;

    // Start is called before the first frame update
    void Start()
    {
        objRenderer = gameObject.GetComponent<Renderer>();
        coll = gameObject.GetComponent<CapsuleCollider>();
        dncontrol = lightControl.GetComponent<DayNightController>();
        home.transform.GetChild(0).GetComponent<HouseSpawn>().AddLights();
        home.transform.GetChild(0).GetComponent<HouseSpawn>().in_me.Add(gameObject);
        at_home = true;
        gameObject.SetActive(false);
    }

    public void initGoToWork()
    {
        if (!gameObject.GetComponent<NavMeshAgent>())
            gameObject.AddComponent<NavMeshAgent>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = 0.5f;
        //agent.destination = office.transform.GetChild(0).gameObject.transform.position;
        path = new NavMeshPath();
        NavMeshHit closest_road = new NavMeshHit();
        NavMeshHit closest_road2 = new NavMeshHit();
        NavMesh.SamplePosition(office.transform.GetChild(0).position, out closest_road, 5f, NavMesh.GetAreaFromName("Roads"));
        NavMesh.SamplePosition(home.transform.GetChild(0).position, out closest_road2, 5f, NavMesh.GetAreaFromName("Roads"));
        office_access = closest_road.position;
        home_access = closest_road2.position;
        //NavMesh.CalculatePath(transform.position, office_access, NavMesh.AllAreas, path);
        //agent.SetPath(path);
    }

    // Update is called once per frame
    void Update()
    {
        if (!home)
        {
            Destroy(gameObject);
            Destroy(this);
        }
        //if (!colored && Time.time<2)
        //{
        //    objRenderer.material.SetColor("_Color", home.transform.GetComponent<HouseSpawn>().build_color);
        //    colored = true;
        //}
        if (!office)
        {
            unemployed = true;
        } 
        else if (going_to_work && Vector3.Distance(transform.position,office_access)<0.8)
        {
            coll.enabled = false;
            agent.isStopped = true;
            gameObject.SetActive(false);
            transform.position = new Vector3(office.transform.GetChild(0).position.x,transform.position.y, office.transform.GetChild(0).position.z);
            at_work = true;
            going_to_work = false;
            print(office);
            HouseSpawn hs = office.transform.GetChild(0).GetComponent<HouseSpawn>();
            hs.AddLights();
            hs.in_me.Add(gameObject);
        }
        else if (going_home && Vector3.Distance(transform.position, home_access) < 0.8)
        {
            coll.enabled = false;
            agent.isStopped = true;
            gameObject.SetActive(false);
            transform.position = new Vector3(home.transform.GetChild(0).position.x, transform.position.y, home.transform.GetChild(0).position.z);
            at_home = true;
            going_home = false;
            print(home);
            HouseSpawn hs = home.transform.GetChild(0).GetComponent<HouseSpawn>();
            hs.AddLights();
            hs.in_me.Add(gameObject);
        }
        else if (!at_work && going_to_work)
        {
            NavMesh.CalculatePath(transform.position, office_access, NavMesh.AllAreas, path);
            agent.SetPath(path);
        }
        else if (!at_home && going_home)
        {
            NavMesh.CalculatePath(transform.position, home_access, NavMesh.AllAreas, path);
            agent.SetPath(path);
        }
    }

    private void OnEnable()
    {
        if (!is_init)
            is_init = true;
        else
        {
            if (going_to_work)
            {
                NavMeshHit closest_road = new NavMeshHit();
                NavMesh.SamplePosition(home.transform.GetChild(0).position, out closest_road, 5f, NavMesh.GetAreaFromName("Roads"));
                home_access = closest_road.position;
                transform.position = new Vector3(home_access.x, transform.position.y, home_access.z);
                agent.isStopped = false;
                at_home = false;
            }
            else if (going_home)
            {
                NavMeshHit closest_road = new NavMeshHit();
                NavMesh.SamplePosition(office.transform.GetChild(0).position, out closest_road, 5f, NavMesh.GetAreaFromName("Roads"));
                office_access = closest_road.position;
                transform.position = new Vector3(office_access.x, transform.position.y, office_access.z);
                agent.isStopped = false;
                at_work = false;
            }
        }
    }
}
