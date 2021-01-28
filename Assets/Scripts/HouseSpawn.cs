using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseSpawn : MonoBehaviour
{
    public int nb_moves = 0;
    public int MAX_MOVES = 3;
    public int PLANE_SIZE = 3;
    public Color build_color;
    private bool colored;
    public GameObject generator;
    public int nb_occup = 0;
    public Material lightmat;
    public Material nolightmat;
    public List<GameObject> in_me;
    public bool workBuilding;
    private DayNightController dncontrol;


    void set_build_color(GameObject go)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (!colored)
        {
            build_color = Random.ColorHSV(0f, 1f, 0.5f, 0.7f, 1f, 1f);
            colored = true;
        }
        rend.material.SetColor("_Color", build_color);
    }



    // Start is called before the first frame update
    void Start()
    {
        dncontrol = GameObject.Find("LightControl").GetComponent<DayNightController>();
        if (transform.childCount < 5)
        {
            workBuilding = false;
            set_build_color(gameObject);
        }
        else
            workBuilding = true;
    }

    public void AddLights()
    {
        if (nb_occup == 0 && transform.childCount > 3)
        {
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = gameObject.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = lightmat;
            }
            nb_occup++;
        }
        else if (nb_occup == 1 && transform.childCount > 4)
        {
            GameObject child1 = gameObject.transform.GetChild(4).gameObject;
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = child1.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = lightmat;
            }
            nb_occup++;
        }
        else if (nb_occup == 2 && transform.childCount > 5)
        {
            GameObject child2 = gameObject.transform.GetChild(5).gameObject;
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = child2.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = lightmat;
            }
            nb_occup++;
        }
    }
    public void RemoveLights()
    {
        if (nb_occup == 1 && transform.childCount > 3)
        {
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = gameObject.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = nolightmat;
            }
            nb_occup--;
        }
        else if (nb_occup == 2 && transform.childCount > 4)
        {
            GameObject child1 = gameObject.transform.GetChild(4).gameObject;
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = child1.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = nolightmat;
            }
            nb_occup--;
        }
        else if (nb_occup == 3 && transform.childCount > 5)
        {
            GameObject child2 = gameObject.transform.GetChild(5).gameObject;
            for (int k = 0; k < 4; k++)
            {
                Renderer rend = child2.transform.GetChild(k).gameObject.GetComponent<Renderer>();
                rend.material = nolightmat;
            }
            nb_occup--;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (in_me.Count>0)
        {
            for (int k = 0; k < in_me.Count; k++)
            {
                if (dncontrol.currentTimeOfDay > 0.3f && dncontrol.currentTimeOfDay < 0.8f && !workBuilding && !in_me[k].activeSelf)
                {
                    in_me[k].GetComponent<CitizenBehaviour>().going_to_work = true;
                    in_me[k].SetActive(true);
                    RemoveLights();
                    in_me.RemoveAt(k);
                }
                else if (dncontrol.currentTimeOfDay > 0.8f && workBuilding && !in_me[k].activeSelf)
                {
                    in_me[k].GetComponent<CitizenBehaviour>().going_home = true;
                    in_me[k].SetActive(true);
                    RemoveLights();
                    in_me.RemoveAt(k);
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (nb_moves < 0)
        {
            nb_moves++;
        }
        else if (other.gameObject.tag == "Building" || other.gameObject.tag == "Road")
        {
            if (nb_moves >= MAX_MOVES)
            {
                if (generator.GetComponent<VoronoiDemo>().officeBuilding.Contains(gameObject))
                    generator.GetComponent<VoronoiDemo>().officeBuilding.Remove(gameObject);
                Destroy(gameObject.transform.parent.gameObject);
                Destroy(this);
            }
            else
            {
                Vector3 p = this.gameObject.transform.position;
                Vector3 p1 = other.gameObject.transform.position;
                Vector3 col_axis = p - p1;
                col_axis.y = 0;
                Vector3 new_position = this.gameObject.transform.position + col_axis / 5;

                if (new_position.x < -PLANE_SIZE * 5 || new_position.x > PLANE_SIZE * 5 ||
                     new_position.z < -PLANE_SIZE * 5 || new_position.z > PLANE_SIZE * 5)
                {
                    if (generator.GetComponent<VoronoiDemo>().officeBuilding.Contains(gameObject))
                        generator.GetComponent<VoronoiDemo>().officeBuilding.Remove(gameObject);
                    Destroy(gameObject.transform.parent.gameObject);
                    Destroy(this);
                    //Destroy(gameObject);
                }
                else
                {
                    nb_moves += 1;
                    this.gameObject.transform.position = new_position;
                }
            }
        }
        //else if (other.gameObject.tag == "Citizen")
        //{
        //    nb_moves++;
        //    if (nb_moves > MAX_MOVES)
        //        Destroy(this);
        //}
        else if (other.gameObject.tag == "Plane")
        {
            if (generator.GetComponent<VoronoiDemo>().officeBuilding.Contains(gameObject))
                generator.GetComponent<VoronoiDemo>().officeBuilding.Remove(gameObject);
            Destroy(gameObject.transform.parent.gameObject);
            Destroy(this);
        }

        //else
        //    print(other.gameObject.tag);
    }
}
