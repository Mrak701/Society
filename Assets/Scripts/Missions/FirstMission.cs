﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstMission : Mission
{
    [SerializeField] private MissionsManager missionsManager;

    [SerializeField] private ChairMesh startChair;
    private Image backgroundWhiteImage;
    private bool messageWasListened = false;
    [ShowIf(nameof(startChair), true)] [SerializeField] private GameObject farewallNote;
    [ShowIf(nameof(startChair), true)] [SerializeField] private Transform fNTransform;
    [ShowIf(nameof(startChair), true)] [SerializeField] private Transform startStayPlace;

    [Space(15)]
    [SerializeField] private GameObject dogsForInstance;
    [ShowIf(nameof(dogsForInstance), true)] [SerializeField] private List<DogEnemy> dogsFI = new List<DogEnemy>();
    [ShowIf(nameof(dogsForInstance), true)] [SerializeField] private DoorManager doorOnSurface;

    public override void StartOrContinueMission(int skipLength)
    {
        if (skipLength == 0)
        {
            CreateBackground();
            PutOnAChair();
            ListenMessage();
            WriteNote();
            Comment(0);
            SetTask(currentTask);
        }
        for (int i = 0; i < skipLength; i++)
        {
            currentTask++;
            SetTask(currentTask);
        }
    }
    public override void FinishMission()
    {
        throw new System.NotImplementedException();
    }
    public override int GetMissionNumber()
    {
        return 0;
    }
    /// <summary>
    /// событие чекпоинта
    /// </summary>
    public override void Report()
    {
        currentTask++;
        missionsManager.ReportTask();
        SetTask(currentTask);
    }
    /// <summary>
    /// игрока садят на табурет
    /// </summary>
    private void PutOnAChair()
    {
        var player = FindObjectOfType<PlayerClasses.PlayerStatements>();
        player.transform.position = startStayPlace.position;
        startChair.Interact(player);
    }
    /// <summary>
    /// на специальном холсте для
    /// спец-эффектов вознкикает
    /// фоновый рисунок и постепенно тает
    /// </summary>
    private void CreateBackground()
    {
        GameObject image = new GameObject("WhiteBackground");
        Canvas effectsCanvas = missionsManager.GetEffectsCanvas();
        image.transform.SetParent(effectsCanvas.transform);
        backgroundWhiteImage = image.AddComponent<Image>();
        backgroundWhiteImage.gameObject.AddComponent<LighteningBackground>().Init(backgroundWhiteImage, 0.25f);
        backgroundWhiteImage.transform.localScale = effectsCanvas.GetComponent<CanvasScaler>().referenceResolution / 100;
        backgroundWhiteImage.transform.localPosition = Vector2.zero;
    }
    /// <summary>
    /// переключается радио и слушается сигнал
    /// </summary>
    private void ListenMessage()
    {
        //TODO: сделать переключение и калибровку радио для прослушивания сообщения       
    }
    /// <summary>
    /// на столе пишется записка
    /// </summary>
    private void WriteNote()
    {
        //TODO: сделать анимацию записывания записки

        //В будущем заменить
        Instantiate(farewallNote, fNTransform.position, fNTransform.rotation);
        //В будущем заменить
    }
    /// <summary>
    /// Главный герой говорит комментарий
    /// </summary>
    /// <param name="i"></param>
    private void Comment(int i)
    {
        string[] allContent = System.IO.File.ReadAllLines(Localization.PathToCurrentLanguageContent(Localization.Type.Dialogs, GetMissionNumber()));
        string neededContent = allContent[i];
        Dialogs.Dialog dialog = new Dialogs.Dialog(neededContent);
        missionsManager.GetDialogDrawer().DrawNewDialog(dialog, 4);
    }
    /// <summary>
    /// назначение задачи 
    /// </summary>
    /// <param name="i"></param>
    private void SetTask(int i)
    {
        string[] allContent = System.IO.File.ReadAllLines(Localization.PathToCurrentLanguageContent(Localization.Type.Tasks, GetMissionNumber()));
        string neededContent;
        try
        {
            neededContent = DecodeTask(allContent[i]);
        }

        catch (System.IndexOutOfRangeException)
        {
            neededContent = "Ошибка при прочтении задания : недостаточно заданий";
        }

        missionsManager.GetTaskDrawer().DrawNewTask(neededContent);
        DoingsInMission.CheckTaskForPossibleDoing(i, this);
    }
    /// <summary>
    /// преобразование текста в нормальный текст, убираются спецсимволы
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private string DecodeTask(string str)
    {
        string retStr = "";
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i].ToString() == @"\" && str[i + 1] == 'n')
            {
                retStr += "\n";
                i++;
            }
            else
                retStr += str[i];
        }
        return retStr;
    }

    static class DoingsInMission
    {
        private const int codeForIFD = 11;// code for instance flock dogs
        private const int codeForECD = 12;// code for extrim close door
        public static void CheckTaskForPossibleDoing(int i, FirstMission mission)
        {
            switch (i)
            {
                case codeForIFD:
                    InstanceFlockDogs(mission);
                    break;
                case codeForECD:
                    ExtrimCloseDoor(mission);
                    break;
            }
        }
        private static void InstanceFlockDogs(FirstMission mission)
        {
            mission.dogsForInstance.SetActive(true);
            foreach(var d in mission.dogsFI)
            {
                d.SetEnemy(mission.missionsManager.GetPlayerBasicNeeds());
                d.SetCurrentEnemyForewer(true);
            }
        }
        private static void ExtrimCloseDoor(FirstMission mission)
        {
            mission.PFT = false;
            mission.doorOnSurface.SetExtrimSituation(true);
            mission.doorOnSurface.SetStateAfterNextInteract(State.locked);            
        }
    }
    private const int dogsToKill = 3;
    private int killedDogs = 0;
    public void KillTheDogs()
    {
        if (++killedDogs == dogsToKill)
        {
            Report();
        }
    }
    private bool PFT = true;// possible finish task
    public bool PossibleMoveToBunker()
    {
        return doorOnSurface.GetState() == State.locked;
    }
}
