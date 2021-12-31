using System.Collections;
using System.Reflection;
using UnityEngine;

public class ModSelectorTablet : MonoBehaviour
{
    public static ModSelectorTablet Instance;

    private const BindingFlags ReflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo HoldableTargetField;
    private static readonly FieldInfo HoldableTargetInitializedField;
    private static readonly MethodInfo HoldableTargetStartMethod;

    private readonly Vector3 SetupPosition = new Vector3(.25f, 1.3f, -1.52f);
    private readonly Vector3 GameplayPosition = new Vector3(.09f, 1.225f, -1.41f);
    
    private readonly Vector3 SetupRotation = new Vector3(13.5f, 0f, 0f);
    private readonly Vector3 GameplayRotation = new Vector3(8f, 0f, 0f);
    
    private void Awake()
    {
        Instance = this;
    }

#if !UNITY_EDITOR
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PageManager.CurrentState != KMGameInfo.State.Transitioning);
        var holdable_target = (MonoBehaviour)HoldableTargetField.GetValue(GetComponent("FloatingHoldable"));
        if(PageManager.CurrentState == KMGameInfo.State.Setup)
        {
            holdable_target.transform.position = SetupPosition;
            holdable_target.transform.rotation = Quaternion.Euler(SetupRotation);
        }
        else
        {
            holdable_target.transform.position = GameplayPosition;
            holdable_target.transform.rotation = Quaternion.Euler(GameplayRotation);
        }
        HoldableTargetInitializedField.SetValue(holdable_target, false);
        HoldableTargetStartMethod.Invoke(holdable_target, new object[0]);
    }

    static ModSelectorTablet()
    {
        HoldableTargetField = ReflectionHelper.FindTypeInGame("FloatingHoldable").GetField("HoldableTarget", ReflectionFlags);
        var HoldableTargetType = HoldableTargetField.FieldType;
        HoldableTargetStartMethod = HoldableTargetType.GetMethod("Start", ReflectionFlags);
        HoldableTargetInitializedField =
            HoldableTargetType.GetField("isInitialized", ReflectionFlags);
    }
#endif
}
