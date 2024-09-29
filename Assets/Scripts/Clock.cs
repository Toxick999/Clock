using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Animator hourHandAn;
    [SerializeField] private Animator minuteHandAn;
    [SerializeField] private Animator secondHandAn;
    [SerializeField] private GameObject reButton;
    [SerializeField] private GameObject settingButton;

    [SerializeField] private TMP_InputField hourInputField;
    [SerializeField] private TMP_InputField minuteInputField;
    [SerializeField] private TMP_InputField secondInputField;

    [SerializeField] private GameObject settingPanel;

    private bool isTimeInvoked = false;
    private DateTime currentTime;
    private int maxAttempts = 10;

    void Start()
    {
        StartCoroutine(GetTimeFromServer());
        InvokeRepeating("HourlyTimeCheck", 3600f, 3600f);

        hourInputField.onEndEdit.AddListener(delegate { OnTimeInputChanged(); });
        minuteInputField.onEndEdit.AddListener(delegate { OnTimeInputChanged(); });
        secondInputField.onEndEdit.AddListener(delegate { OnTimeInputChanged(); });
    }

    IEnumerator GetTimeFromServer()
    {
        string url = "https://worldtimeapi.org/api/timezone/Europe/Moscow";
        int attempts = 0;

        timeText.text = "Connecting...";

        reButton.SetActive(false);
        settingButton.SetActive(false);

        while (attempts < maxAttempts)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        TimeData timeData = JsonUtility.FromJson<TimeData>(webRequest.downloadHandler.text);
                        currentTime = DateTime.Parse(timeData.datetime);
                        Debug.Log("Current server time: " + currentTime);
                        UpdateClockHands();
                        reButton.SetActive(true);
                        settingButton.SetActive(true);
                        if (!isTimeInvoked)
                        {
                            InvokeRepeating("UpdateTime", 0f, 1f);
                            isTimeInvoked = true;
                        }
                        yield break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error parsing time data: " + e.Message);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("Attempt " + (attempts + 1) + " failed: " + webRequest.error);
                    attempts++;
                    yield return new WaitForSeconds(3);
                }
            }
        }

        Debug.LogError("Unable to complete SSL connection after " + maxAttempts + " attempts.");
        reButton.SetActive(true);
        settingButton.SetActive(true);
    }

    void UpdateTime()
    {
        currentTime = currentTime.AddSeconds(1);
        timeText.text = currentTime.ToString("HH:mm:ss");
        UpdateClockHands();
    }

    public void HourlyTimeCheck()
    {
        StartCoroutine(GetTimeFromServer());
    }

    [Serializable]
    public class TimeData
    {
        public string datetime;
    }

    private void UpdateClockHands()
    {
        secondHandAn.Play("Walking", 0, (float)currentTime.Second / 60);
        minuteHandAn.Play("Walking", 0, (float)(currentTime.Minute) / 60 + (float)(currentTime.Second) / 3600);
        hourHandAn.Play("Walhing", 0, (float)(currentTime.Hour % 12) / 12 + (float)(currentTime.Minute) / 720);
    }

    public void OnTimeInputChanged()
    {
        int hours = ParseInputField(hourInputField, 0, 23);
        int minutes = ParseInputField(minuteInputField, 0, 59);
        int seconds = ParseInputField(secondInputField, 0, 59);

        currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, hours, minutes, seconds);
        timeText.text = currentTime.ToString("HH:mm:ss");
        UpdateClockHands();
    }

    private int ParseInputField(TMP_InputField inputField, int minValue, int maxValue)
    {
        int value;
        if (int.TryParse(inputField.text, out value))
        {
            value = Mathf.Clamp(value, minValue, maxValue);
        }
        else
        {
            value = minValue;
        }
        return value;
    }

    public void SettingButton()
    {
        settingPanel.SetActive(!settingPanel.activeSelf);
    }
}
