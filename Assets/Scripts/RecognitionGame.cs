using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using PDollarGestureRecognizer;

public class RecognitionGame : MonoBehaviour {

	public Transform gestureOnScreenPrefab;

    public SpriteRenderer targetSprite;

    public GameObject Cursor;

    public GameObject gameOverScreen;
    public GameObject recordLabel;
    public GameObject scoreTable;

    public UnityEngine.UI.Text scoreCounter;
    public RectTransform timerBar;

    public float initialTimeSetting = 3000f;
    public float roundTimeDecrement = 200f;

    private int bestScore;
    private int currentScore;

    private float timer;
    private float roundTime;

    private GameObject currentCursor;

    enum GameState {Playing, GameOver};

    private GameState gameState;

	private List<Gesture> trainingSet = new List<Gesture>();

    private Gesture currentTargetGesture;

	private List<Point> points = new List<Point>();
	private int strokeId = -1;

	private Vector3 virtualKeyPosition = Vector2.zero;
	private Rect drawArea;

	private RuntimePlatform platform;
	private int vertexCount = 0;

	private List<LineRenderer> gestureLinesRenderer = new List<LineRenderer>();
	private LineRenderer currentGestureLineRenderer;

	private bool recognized;

	void Start () {

		platform = Application.platform;
		drawArea = new Rect(50, 50, 600, 650);

		//Load pre-made gestures
		TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/Premade/");
		foreach (TextAsset gestureXml in gesturesXml)
			trainingSet.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        bestScore = PlayerPrefs.GetInt("BestScore");

        StartGame();

        }

    public void StartGame()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
        gameState = GameState.Playing;
        roundTime = initialTimeSetting/1000 + roundTimeDecrement/1000;
        currentScore = 0;
        NextRound();
    }

    public void GameOver()
    {
        if (gameOverScreen != null)
        {
            
            {
                recognized = false;
                strokeId = -1;

                points.Clear();

                foreach (LineRenderer lineRenderer in gestureLinesRenderer)
                {

                    lineRenderer.SetVertexCount(0);
                    Destroy(lineRenderer.gameObject);
                }

                gestureLinesRenderer.Clear();
            }

            gameOverScreen.SetActive(true);
            recordLabel.SetActive(false);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                recordLabel.SetActive(true);
                PlayerPrefs.SetInt("BestScore", (int)bestScore);
            }
            scoreTable.GetComponent<UnityEngine.UI.Text>().text = String.Format("Score: {0}\nBest Score: {1}", currentScore, bestScore);


        }
    }

    void NextRound()
    {
        roundTime = roundTime - roundTimeDecrement/1000;
        timer = roundTime;
        currentTargetGesture = getTargetGesture();
    }

	void Update () {

        if (timer <= 0&&gameState==GameState.Playing)
        {
            gameState = GameState.GameOver;
            GameOver();
        }

        if (gameState==GameState.Playing)
        {
            timer = (timer - Time.deltaTime);
            timerBar.localScale = new Vector3(timer / roundTime, 1, 1);

            scoreCounter.text = currentScore.ToString();
            
            if (platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount > 0)
                {
                    virtualKeyPosition = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
                }
            }
            else {
                if (Input.GetMouseButton(0))
                {
                    virtualKeyPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
                }
            }

            if (drawArea.Contains(virtualKeyPosition))
            {

                if (Input.GetMouseButtonDown(0))
                {

                    currentCursor = Instantiate(Cursor, new Vector3(virtualKeyPosition.x, virtualKeyPosition.y, 10), new Quaternion(0, 0, 0, 1)) as GameObject;
                    currentCursor.SetActive(true);

                    if (recognized)
                    {

                        recognized = false;
                        strokeId = -1;

                        points.Clear();

                        foreach (LineRenderer lineRenderer in gestureLinesRenderer)
                        {

                            lineRenderer.SetVertexCount(0);
                            Destroy(lineRenderer.gameObject);
                        }

                        gestureLinesRenderer.Clear();
                    }

                    ++strokeId;

                    Transform tmpGesture = Instantiate(gestureOnScreenPrefab, transform.position, transform.rotation) as Transform;
                    currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();

                    gestureLinesRenderer.Add(currentGestureLineRenderer);

                    vertexCount = 0;
                }

                if (Input.GetMouseButton(0))
                {

                    currentCursor.GetComponent<Transform>().localPosition = Camera.main.ScreenToWorldPoint(new Vector3(virtualKeyPosition.x, virtualKeyPosition.y, 10));

                    points.Add(new Point(virtualKeyPosition.x, -virtualKeyPosition.y, strokeId));

                    currentGestureLineRenderer.SetVertexCount(++vertexCount);
                    currentGestureLineRenderer.SetPosition(vertexCount - 1, Camera.main.ScreenToWorldPoint(new Vector3(virtualKeyPosition.x, virtualKeyPosition.y, 10)));
                }

                if (Input.GetMouseButtonUp(0))
                {
                    currentCursor.SetActive(false);
                    Destroy(currentCursor);

                    recognized = true;

                    Gesture candidate = new Gesture(points.ToArray());
                    Result gestureResult = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

                    if (gestureResult.GestureClass == currentTargetGesture.Name)
                    {
                        currentGestureLineRenderer.GetComponent<Animator>().Play("CorrectLine");
                        currentScore = currentScore + (int) (timer*1000);
                        NextRound();
                    }
                    else
                    {
                        currentGestureLineRenderer.GetComponent<Animator>().Play("IncorrectLine");
                    }
                }
            } 
        }
	}

    Gesture getTargetGesture()
    {
        int targetIndex = 0;
        do {
            targetIndex = UnityEngine.Random.Range(0, trainingSet.Count);
        } while (trainingSet[targetIndex] == currentTargetGesture); // make sure to pick different gesture every time.

        targetSprite.sprite = Resources.Load<Sprite>(trainingSet[targetIndex].Name);
        return trainingSet[targetIndex];
    }
}
