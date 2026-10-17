using TMPro;
using UnityEngine;

public sealed class Stage02HUD : MonoBehaviour
{
    [SerializeField] private TMP_Text goalLabel;
    [SerializeField] private RotationModule turntable;
    [SerializeField] private PressurePlate shadowPlate;
    [SerializeField] private StageGate shadowGate;
    [SerializeField] private EnvironmentalHazard hazard;
    [SerializeField] private ShadowController shadowController;
    [SerializeField] private StageExit exit;

    public void Configure(TMP_Text text, RotationModule rotation, PressurePlate plate,
        StageGate gate,
        EnvironmentalHazard environmentalHazard, ShadowController shadow, StageExit stageExit)
    {
        goalLabel = text;
        turntable = rotation;
        shadowPlate = plate;
        shadowGate = gate;
        hazard = environmentalHazard;
        shadowController = shadow;
        exit = stageExit;
    }

    private void Update()
    {
        if (goalLabel == null) return;
        if (exit != null && exit.IsComplete)
            goalLabel.text = "第二关完成 · 抵达观测台";
        else if (turntable != null && turntable.CurrentIndex == 0)
            goalLabel.text = "靠近黄铜转盘按 E，让断开的格子接上";
        else if (shadowGate != null && shadowGate.IsOpen)
            goalLabel.text = hazard != null && hazard.IsSpent
                ? "巡逻者已清除 · 点击终点继续"
                : "道路已开 · 靠近陷阱装置按 E 清除巡逻者";
        else if (shadowController != null && shadowController.HasShadow)
            goalLabel.text = "沿格子前进，让影子踏上青色机关";
        else
            goalLabel.text = "按住 F 查看可放影子的格子，松开 F 投放";
    }
}
