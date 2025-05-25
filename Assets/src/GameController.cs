using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class GameController : MonoBehaviour
{
    public GameObject[] objectsToMemorize; // 5个需要记忆的物体
    public Text timerText;
    public Text roundText;
    public TextMeshProUGUI whiteNodeText;
    public GameObject startDlg;
    public GameObject resultDlg;
    public GameObject whiteNode;
    public Text resultText;

    private int[] currentSlotIndices; // 当前每个物体所在的槽位索引（0-9，0-4为上方，5-9为下方）
    private int[] initialIndices;    // 初始摆放位置排列
    private int[] correctIndicesRound1; // 第一轮正确索引顺序
    private int[] correctIndicesRound2; // 第二轮正确索引顺序

    private int draggedObjectIndex = -1;

    private int currentRound = 0; // 0=准备,1=记忆1,2=游戏1,3=记忆2,4=游戏2,5=测试1,6=测试2,7结束
    private float timer = 0f;
    private int correctCount = 0;
    private int[] roundScores = new int[7];

    private bool isDragging = false;
    private GameObject draggedObject;
    private Vector3 offset;

    // 定义所有10个槽位（0-4为上方桌面，5-9为下方桌底）
    private Vector3[] slotPositions = new Vector3[] {
        // 上方桌面的5个位置
        new Vector3(0.7f, 1.32f, 0),
        new Vector3(0, 1.32f, 0),
        new Vector3(-0.6f, 1.32f, 0),
        new Vector3(0.5f, 0.8f, 0),
        new Vector3(-0.5f, 0.8f, 0),
        // 下方桌底的5个位置
        new Vector3(-1.0f, 0.1f, 0),
        new Vector3(-0.5f, 0.1f, 0),
        new Vector3(0, 0.1f, 0),
        new Vector3(0.5f, 0.1f, 0),
        new Vector3(1.0f, 0.1f, 0)
    };

    void Start()
    {
        // 初始化UI
        whiteNode.SetActive(false);
        startDlg.SetActive(true);
        resultDlg.SetActive(false);
    }

    IEnumerator ShowWhiteScreenInReadyStage()
    {
        whiteNode.SetActive(true);
        yield return new WaitForSeconds(GameConfig.Instance.WhiteScreenTime);
        whiteNode.SetActive(false);
    }

    public void onBtnStartClick()
    {
        currentRound = 1;
        StartCoroutine(ShowWhiteScreenInReadyStage());
        StartCoroutine(MemorizeRound());
        startDlg.SetActive(false);
    }

    public void onBtnNextClick()
    {
        resultDlg.SetActive(false);

        // 进入下一轮或测试
        if (currentRound == 2)
        {
            whiteNodeText.text = "Round2";
            StartCoroutine(ShowWhiteScreenInReadyStage());
            currentRound = 3;
            StartCoroutine(MemorizeRound());
        }
        else if (currentRound == 4)
        {
            currentRound = 5;
            StartCoroutine(TestRound());
        }
        else if (currentRound == 5)
        {
            currentRound = 6;
            StartCoroutine(TestRound());
        }
        else
        {
            // 游戏结束，保存分数
            SaveScoresToFile();
            currentRound = 7;
            roundText.text = "Game Over";
            timerText.text = "";
        }
    }

    IEnumerator MemorizeRound()
    {
        // 记忆阶段：物体在上方桌面位置
        currentSlotIndices = GenerateRandomIndices(0, 4); // 只在上方位置0-4

        // 生成初始位置排列（在下方位置5-9）
        do
        {
            initialIndices = GenerateRandomIndices(5, 9); // 只在下方位置5-9
        } while (!AreAllPositionsDifferent(currentSlotIndices, initialIndices));

        // 保存当前轮次的正确位置
        if (currentRound == 1)
        {
            correctIndicesRound1 = (int[])currentSlotIndices.Clone();
        }
        else if (currentRound == 3)
        {
            correctIndicesRound2 = (int[])currentSlotIndices.Clone();
        }

        UpdateObjectPositions();

        // 显示记忆倒计时
        timer = GameConfig.Instance.MemoryTime;
        while (timer > 0)
        {
            timerText.text = Mathf.CeilToInt(timer).ToString() + "s";
            timer -= Time.deltaTime;
            yield return null;
        }

        // 将物体移到下方位置
        currentSlotIndices = (int[])initialIndices.Clone();
        UpdateObjectPositions();

        whiteNode.SetActive(true);
        whiteNodeText.text = "元の場所にもどそう！";
        yield return new WaitForSeconds(GameConfig.Instance.WhiteScreenTime);
        whiteNode.SetActive(false);

        // 进入游戏回合
        currentRound++;
        StartCoroutine(GameRound());
    }

    IEnumerator GameRound()
    {
        // 开始游戏倒计时
        timer = GameConfig.Instance.GameTime;
        while (timer > 0)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timer).ToString() + "s";
            timer -= Time.deltaTime;
            yield return null;
        }

        // 计算正确数量（只计算在上方正确位置的物体）
        correctCount = CalculateCorrectPositions();
        roundScores[currentRound] = correctCount;
        ShowResult();
    }

    IEnumerator TestRound()
    {
        roundText.text = currentRound == 5 ? "Test Round 1" : "Test Round 2";

        // 测试轮：直接将物体放在下方位置
        if (currentRound == 5)
        {
            do
            {
                initialIndices = GenerateRandomIndices(5, 9); // 只在下方位置5-9
            } while (!AreAllPositionsDifferent(correctIndicesRound1, initialIndices));
        }
        else if (currentRound == 6)
        {
            do
            {
                initialIndices = GenerateRandomIndices(5, 9); // 只在下方位置5-9
            } while (!AreAllPositionsDifferent(correctIndicesRound2, initialIndices));
        }

        currentSlotIndices = initialIndices;
        UpdateObjectPositions();

        // 开始游戏倒计时
        timer = GameConfig.Instance.GameTime;
        while (timer > 0)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timer).ToString() + "s";
            timer -= Time.deltaTime;
            yield return null;
        }

        // 计算正确数量
        correctCount = CalculateCorrectPositions();
        roundScores[currentRound] = correctCount;

        // 显示结果
        ShowResult();
    }

    // 根据当前索引更新物体位置
    void UpdateObjectPositions()
    {
        for (int i = 0; i < objectsToMemorize.Length; i++)
        {
            objectsToMemorize[i].transform.position = slotPositions[currentSlotIndices[i]];
        }
    }

    // 生成随机索引（在指定范围内）
    int[] GenerateRandomIndices(int startIndex, int endIndex)
    {
        List<int> indices = new List<int>();
        for (int i = startIndex; i <= endIndex; i++)
        {
            indices.Add(i);
        }

        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        return indices.ToArray();
    }

    // 检查两个排列是否所有位置都不同（只比较原始索引0-4）
    bool AreAllPositionsDifferent(int[] arr1, int[] arr2)
    {
        for (int i = 0; i < arr1.Length; i++)
        {
            // 将下方位置映射到对应的上方位置进行比较
            int pos1 = arr1[i] % 5;
            int pos2 = arr2[i] % 5;
            if (pos1 == pos2) return false;
        }
        return true;
    }

    int CalculateCorrectPositions()
    {
        int count = 0;
        int[] correctIndices = currentRound < 5 ?
            (currentRound == 2 ? correctIndicesRound1 : correctIndicesRound2) :
            (currentRound == 5 ? correctIndicesRound1 : correctIndicesRound2);

        for (int i = 0; i < objectsToMemorize.Length; i++)
        {
            // 只有当物体在上方位置（0-4）且位置正确时才计分
            if (currentSlotIndices[i] < 5 && currentSlotIndices[i] == correctIndices[i])
            {
                count++;
            }
        }

        return count;
    }

    void ShowResult()
    {
        roundText.text = "";
        timerText.text = "";
        if (currentRound == 2 || currentRound == 4)
        {
            resultText.text = "Score: " + correctCount + "/" + objectsToMemorize.Length;
        }
        else
        {
            resultText.text = "";
        }
        resultDlg.SetActive(true);
    }

    void SaveScoresToFile()
    {
        string path = Application.dataPath + "/GameScores.txt";
        string content = "Play Round 1: " + roundScores[2] + "/5\n" +
                         "Play Round 2: " + roundScores[4] + "/5\n" +
                         "Test Round 1: " + roundScores[5] + "/5\n" +
                         "Test Round 2: " + roundScores[6] + "/5";

        File.WriteAllText(path, content);
        Debug.Log("Scores saved to: " + path);
    }

    void Update()
    {
        if (currentRound == 2 || currentRound == 4 || currentRound == 5 || currentRound == 6)
        {
            HandleDragAndDrop();
        }
    }

    private void HandleDragAndDrop()
    {
        int targetLayer = LayerMask.GetMask("CtrlObj");

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, targetLayer))
            {
                for (int i = 0; i < objectsToMemorize.Length; i++)
                {
                    if (hit.collider.gameObject == objectsToMemorize[i])
                    {
                        isDragging = true;
                        draggedObject = objectsToMemorize[i];
                        draggedObjectIndex = i;
                        offset = draggedObject.transform.position - hit.point;
                        offset.z = 0;
                        break;
                    }
                }
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            // 获取鼠标世界坐标
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                Camera.main.WorldToScreenPoint(draggedObject.transform.position).z));

            // 保持原始Z坐标，只更新XY
            Vector3 newPosition = mouseWorldPos + offset;
            newPosition.z = draggedObject.transform.position.z; // 锁定Z轴

            draggedObject.transform.position = newPosition;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            // 设定吸附距离阈值
            float snapDistance = 0.3f;

            // 找到最近的槽位
            int nearestSlotIndex = -1;
            float minDistance = float.MaxValue;

            for (int i = 0; i < slotPositions.Length; i++)
            {
                // 检查该槽位是否已被占用
                bool slotOccupied = false;
                for (int j = 0; j < objectsToMemorize.Length; j++)
                {
                    if (j != draggedObjectIndex && currentSlotIndices[j] == i)
                    {
                        slotOccupied = true;
                        break;
                    }
                }

                if (!slotOccupied)
                {
                    float distance = Vector3.Distance(draggedObject.transform.position, slotPositions[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestSlotIndex = i;
                    }
                }
            }

            // 如果找到空槽位且距离在阈值内，才吸附到该位置
            if (nearestSlotIndex != -1 && minDistance <= snapDistance)
            {
                currentSlotIndices[draggedObjectIndex] = nearestSlotIndex;
                UpdateObjectPositions();
            }
            else
            {
                // 如果没有符合条件的槽位，回到原来的位置
                UpdateObjectPositions(); // 这会让物体回到 currentSlotIndices[draggedObjectIndex] 指定的位置
            }

            isDragging = false;
            draggedObject = null;
            draggedObjectIndex = -1;
        }
    }
}