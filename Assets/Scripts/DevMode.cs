using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DevMode : MonoBehaviour
{
    [SerializeField] float slowness = 10;
    bool isGameSlow = false;

    bool isDrawing = false;

    [SerializeField] GameObject brush;
    LineRenderer currentLineRenderer;

    Vector2 lastPos;
    
    public float lineDist = 0;

    [SerializeField] float lengthBeforeUseConsumed = 10;

    AbilityMeter designMeter;
    AbilityMeter programmingMeter;
    AbilityMeter artMeter;

    [SerializeField] GameObject designerScreen;
    [SerializeField] GameObject programmerScreen;
    [SerializeField] GameObject artScreen;

    float pointCost = 0;

    TMP_InputField programmingInputField;

    public enum DevType
    {
        None,
        Programming,
        Design,
        Art,
    }
    DevType devType = DevType.None;

    public bool IsGameSlow() { return isGameSlow; }

    private void Awake()
    {
        foreach (AbilityMeter meter in FindObjectsOfType<AbilityMeter>())
        {
            if (meter.abilityType == AbilityMeter.AbilityType.Design)
            {
                designMeter = meter;
            }
            else if (meter.abilityType == AbilityMeter.AbilityType.Programming)
            {
                programmingMeter = meter;
            }
            else if (meter.abilityType == AbilityMeter.AbilityType.Art)
            {
                artMeter = meter;
            }
        }

        programmingInputField = programmerScreen.transform.Find("TextBox").GetComponent<TMP_InputField>();
    }

    public void HideDevUIs()
    {
        designerScreen.SetActive(false);
        programmerScreen.SetActive(false);
        artScreen.SetActive(false);
    }

    public void EnterDevMode()
    {
        HideDevUIs();
        
        isGameSlow = true;
        foreach (Movement movement in FindObjectsOfType<Movement>())
        {
            movement.SetSlowness(slowness);
            movement.GetComponent<Attack>().canAttack = false;
        }
    }

    public void OnExitDevMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ExitDevMode();
        }
    }

    public void ExitDevMode()
    {
        devType = DevType.None;
        isGameSlow = false;
        foreach (Movement movement in FindObjectsOfType<Movement>())
        {
            movement.SetSlowness(1);
            movement.GetComponent<Attack>().canAttack = true;
        }
        lineDist = 0;
        HideDevUIs();
    }

    public void EnterArtDevMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (devType != DevType.Art && artMeter.abilityPoints > 0)
            {
                EnterDevMode();
                lineDist = 0;
                devType = DevType.Art;
                artScreen.SetActive(true);
            }
            else
            {
                ExitDevMode();
            }
        }
    }

    public void EnterProgrammerDevMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (devType != DevType.Programming && programmingMeter.abilityPoints > 0)
            {
                EnterDevMode();
                devType = DevType.Programming;
                programmerScreen.SetActive(true);
                programmerScreen.GetComponentInChildren<TMP_InputField>().ActivateInputField();
            }
//          else
//          {
//              ExitDevMode();
//          }
        }
    }

    public void EnterDesignerDevMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (devType != DevType.Design && designMeter.abilityPoints > 0)
            {
                EnterDevMode();
                devType = DevType.Design;
                designerScreen.SetActive(true);
            }
            else
            {
                ExitDevMode();
            }
        }
    }

    public void Draw(InputAction.CallbackContext context)
    {
        if (context.performed && devType == DevType.Art)
        {
            CreateBrush();
            isDrawing = true;
        }

        if (context.canceled && devType == DevType.Art)
        {
            CompleteLine();
        }
    }

    public void CompleteLine()
    {
        if (currentLineRenderer)
        {
            artMeter.SpendAbilityPoints((int)pointCost);
            currentLineRenderer.GetComponent<DrawnLine>().GenerateLineCollider();
        }

        currentLineRenderer = null;
        isDrawing = false;
        lineDist = 0;
        lastPos = Vector2.zero;

        if (artMeter.GetAbilityPoints() == 0)
        {
            ExitDevMode();
        }
    }

    public void CreateBrush()
    {
        GameObject brushInstance = Instantiate(brush);
        currentLineRenderer = brushInstance.GetComponent<LineRenderer>();

        Vector2 mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        currentLineRenderer.SetPosition(0, mousePos);
        currentLineRenderer.SetPosition(1, mousePos);

    }

    public void Update()
    {
        if (devType == DevType.Art && isDrawing)
        {
            Vector2 mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (mousePos != lastPos)
            {
                pointCost = (lineDist / lengthBeforeUseConsumed) + 1;
                if (pointCost < artMeter.GetAbilityPoints() + 1)
                {
                    artMeter.SetHighlightPoints((int)pointCost);
                    if (lastPos != Vector2.zero)
                    {
                        lineDist += (mousePos - lastPos).magnitude;
                    }

                    AddPoint(mousePos);
                    lastPos = mousePos;
                }
                else
                {
                    CompleteLine();
                    ExitDevMode();
                }
            }
            
            //Instantiate(artCirclePrefab, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity);
        }
    }

    public void AddPoint(Vector2 pointPos)
    {
        currentLineRenderer.positionCount++;
        int positionIndex = currentLineRenderer.positionCount - 1;
        currentLineRenderer.SetPosition(positionIndex, pointPos);
    }

    int pointsAssigned = 0;
    public void Confirm(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (devType == DevType.Programming)
            {
                if (programmerScreen.transform.Find("TextBox"))
                {
                    if (programmingInputField.isFocused)
                    {
                        if (CheckCommand("hp += ", ";"))
                        {
                            Debug.Log("HP increased by " + pointsAssigned);
                        }
                        else if (CheckCommand("time.Slow(", ");"))
                        {
                            Debug.Log("Time slowed by " + pointsAssigned);
                        }

                        programmingInputField.text = "";
                        ExitDevMode();
                    }
                }
            }
        }
    }

    public bool CheckCommand(string _commandInput, string _endCommandInput)
    {
        pointsAssigned = 0;

        if ((programmingInputField.text.Contains(_commandInput) && programmingInputField.text.Contains(_endCommandInput)) && programmingInputField.text[0] == _commandInput[0])
        {
            programmingInputField.text = programmingInputField.text.Replace(_commandInput, "");

            try
            {
                pointsAssigned = int.Parse(programmingInputField.text);
            }
            catch
            {
                try
                {
                    if (programmingInputField.text[programmingInputField.text.Length - _endCommandInput.Length] == _endCommandInput[0])
                    {
                        programmingInputField.text = programmingInputField.text.Replace(_endCommandInput, "");

                        pointsAssigned = int.Parse(programmingInputField.text);
                    }
                }
                catch
                {

                }
            }

            if (pointsAssigned != 0)
            {
                return true;
            }

        }

        return false;
    }

}
