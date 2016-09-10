﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public struct Stage
{
    public Wave[] waves;
}

public class GameManager : MonoBehaviour {

    public GameObject player;
    public Text guiTextScore;
    public int numScoreDigit;
    public float stageInterval;

    public Stage[] galaxies;
    public ExpCrystal[] expCrystals;
    public HealCrystal[] healCrystals;

    public BackgroundScroller BgScroller;
    public GameObject UiMenu;
    public GameObject UiHowToPlay;
    public GameObject UiHud;
    public GameObject UiGameover;
    public Text UiTextDisplay;

    private int _score;
    private float _difficultyFactor;
    private int _stage;
    private bool _enabledEnterRestart;

	void Start ()
    {
        // show main menu
        showMainMenu();
    }

    void Update()
    {
        // let player press enter to start
        if (_enabledEnterRestart && Input.GetButtonDown("Submit"))
            restart();
    }

    public void showMainMenu()
    {
        // disable other UI and enable main menu
        UiMenu.SetActive(true);
        UiHowToPlay.SetActive(false);
        UiHud.SetActive(false);
        UiGameover.SetActive(false);
        _enabledEnterRestart = true;
    }
    public void showHowToPlay()
    {
        // enable how to play
        UiMenu.SetActive(false);
        UiHowToPlay.SetActive(true);
    }

    public void restart()
    {
        // disable enter
        _enabledEnterRestart = false;

        // clear previous remains
        clearRemainingGameObjects();

        // disable other UI and enable HUD
        UiMenu.SetActive(false);
        UiHud.SetActive(true);
        UiGameover.SetActive(false);

        // stop all previous coroutines
        StopAllCoroutines();

        // initial properties
        _score = 0;
        _difficultyFactor = 1.0f;
        _stage = 1;

        // update score UI
        updateScoreUI(_score);

        // spawn player
        Instantiate(player, player.transform.position, player.transform.rotation);

        // spawn waves
        StartCoroutine(spawnWaves());
    }

    public void gameover()
    {
        // stop all coroutines
        StopAllCoroutines();

        // TODO: check highscores

        // set the gameover menu active
        UiGameover.SetActive(true);
        _enabledEnterRestart = true;
    }

    public void addScore(int num)
    {
        // add to total score (with difficulty bonus)
        _score += (int)(num * _difficultyFactor);

        // update score UI
        updateScoreUI(_score);
    }

    public void dropExperience(Vector3 center, float radius, int exp)
    {
        // sort the crystals from small to big
        //Array.Sort(expCrystals); // TODO: IComparable

        // random generate the combinations of crystals
        while(exp >= expCrystals[0].experience)
        {
            // find the index of the biggest crystal suitable
            int indexMax = 0;
            for(int i = 0; i < expCrystals.Length; i++)
            {
                if(exp >= expCrystals[i].experience)
                    indexMax = i;
            }

            // random suitable crystal
            ExpCrystal crystal = expCrystals[UnityEngine.Random.Range(0, indexMax + 1)];
            exp -= crystal.experience;

            // random position within a circle
            Vector2 posInUnitCircle = UnityEngine.Random.insideUnitCircle;
            Vector3 position = center + new Vector3(posInUnitCircle.x, 0.0f, posInUnitCircle.y);

            // instantiate game object
            Instantiate(crystal, position, crystal.transform.rotation);
        }
    }

    public void dropHealing(Vector3 center, float radius)
    {
        // non-uniform random healing drop
        List<int> listWeight = new List<int>();
        foreach (HealCrystal heal in healCrystals)
            listWeight.Add(heal.lootWeight);
        int index = getIndexOfNonUniformRandom(listWeight);

        // random position within a circle
        Vector2 posInUnitCircle = UnityEngine.Random.insideUnitCircle;
        Vector3 position = center + new Vector3(posInUnitCircle.x, 0.0f, posInUnitCircle.y);

        // instantiate game object
        HealCrystal crystal = healCrystals[index];
        Instantiate(crystal, position, crystal.transform.rotation);
    }

    private IEnumerator spawnWaves()
    {
        // for each galaxy
        foreach (Stage galaxy in galaxies)
        {
            // stage display start
            UiTextDisplay.text = "Galaxy " + _stage.ToString();
            UiTextDisplay.gameObject.SetActive(true);
            BgScroller.isBoostEnabled = true;

            // stage display end
            yield return new WaitForSeconds(stageInterval);
            UiTextDisplay.gameObject.SetActive(false);
            BgScroller.isBoostEnabled = false;

            // spawn waves one by one
            foreach (Wave wave in galaxy.waves)
            {
                if (wave != null)
                {
                    GameObject objWave = Instantiate(wave.gameObject);
                    objWave.GetComponent<Wave>().spawn(_difficultyFactor);
                    yield return new WaitForSeconds(wave.duration);
                    Destroy(objWave);
                }
            }

            // next atage
            _stage++;
        }
    }

    private void updateScoreUI(int newScore)
    {
        // compensate prefix 0s
        string strScore = newScore.ToString();
        while (numScoreDigit - strScore.Length > 0)
            strScore = "0" + strScore;

        // update to GUI
        guiTextScore.text = strScore;
    }

    private void clearRemainingGameObjects()
    {
        // get all the objects to be cleared
        List<GameObject[]> listObjects = new List<GameObject[]>();
        listObjects.Add(GameObject.FindGameObjectsWithTag("Enemy"));
        listObjects.Add(GameObject.FindGameObjectsWithTag("Bullet"));
        listObjects.Add(GameObject.FindGameObjectsWithTag("Powerup"));
        listObjects.Add(GameObject.FindGameObjectsWithTag("Asteroid"));

        // clear these objects
        foreach(GameObject[] objs in listObjects)
        {
            foreach(GameObject obj in objs)
            {
                Destroy(obj);
            }
        }
    }

    private int getIndexOfNonUniformRandom(List<int> weights)
    {
        // total
        int total = 0;
        foreach (int w in weights)
            total += w;

        // roll a random number and map to the distribution
        int random = Random.Range(1, total + 1);
        int upper = 0;
        for(int i = 0; i < weights.Count; i++)
        {
            upper += weights[i];
            if (random <= upper)
                return i;
        }

        // Error
        Debug.LogError("There is something wrong with the weights.");
        return -1;
    }
}