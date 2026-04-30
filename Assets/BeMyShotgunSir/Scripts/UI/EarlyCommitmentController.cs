using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class EarlyCommitmentController : MonoBehaviour
    {
        [SerializeField] private UIDocument _hudDocument;

        #region Visual Elements
        private VisualElement _root;
        private VisualElement _earlyCommitmentContainer;
        private VisualElement _earlyCommitment;

        private VisualElement _contentArea;
        private VisualElement _earlyCommitmentLabel;
        private VisualElement _commitmentChoice;
        private VisualElement _arrowLeft;
        private VisualElement _arrowRight;
        private VisualElement _commitBarMaskLeft;
        private VisualElement _commitBarMaskRight;

        #endregion

        #region Public Properties
        public float CommitmentValue;// { get; private set; }
        public bool Show;// { get; private set; }
        #endregion


        private void OnEnable()
        {
            _root = _hudDocument.rootVisualElement;
            _earlyCommitmentContainer = _root.Q<VisualElement>("EarlyCommitmentContainer");
            _earlyCommitment = _root.Q<TemplateContainer>("EarlyCommitment");
            _contentArea = _earlyCommitment.Q<VisualElement>("ContentArea");
            _earlyCommitmentLabel = _earlyCommitment.Q<VisualElement>("EarlyCommitmentLabel");
            _commitmentChoice = _earlyCommitment.Q<VisualElement>("CommitmentChoice");
            _arrowLeft = _commitmentChoice.Q<VisualElement>("ArrowLeft");
            _arrowRight = _commitmentChoice.Q<VisualElement>("ArrowRight");
            _commitBarMaskLeft = _arrowLeft.Q<VisualElement>("CommitBarMaskLeft");
            _commitBarMaskRight = _arrowRight.Q<VisualElement>("CommitBarMaskRight");

            StartCoroutine(InitNextFrame());

            _earlyCommitmentContainer.pickingMode = PickingMode.Ignore;
            _earlyCommitment.pickingMode = PickingMode.Ignore;
            _earlyCommitment.Query<VisualElement>().ForEach(el => el.pickingMode = PickingMode.Ignore);

            _arrowLeft.pickingMode = PickingMode.Position;
            _arrowRight.pickingMode = PickingMode.Position;

            _earlyCommitmentContainer.style.display = DisplayStyle.None;
        }

        IEnumerator InitNextFrame()
        {
            yield return null;

            _arrowLeft.RegisterCallback<PointerDownEvent>(ArrowLeftHandler);
            _arrowRight.RegisterCallback<PointerDownEvent>(ArrowRightHandler);

        }

        private void Update()
        {
            if (Show) ShowEarlyCommitment();
            else HideEarlyCommitment();
        }

        private void OnDisable()
        {
            _arrowLeft.UnregisterCallback<PointerDownEvent>(ArrowLeftHandler);
            _arrowRight.UnregisterCallback<PointerDownEvent>(ArrowRightHandler);
        }

        #region Handlers
        private void ArrowLeftHandler(PointerDownEvent evt)
        {
            Debug.Log("Left Arrow Pressed");
        }

        private void ArrowRightHandler(PointerDownEvent evt)
        {
            Debug.Log("Right Arrow Pressed");
        }

        #endregion

        private void ShowEarlyCommitment()
        {
            _earlyCommitmentContainer.style.display = DisplayStyle.Flex;

            float fill = Mathf.Clamp01(CommitmentValue);

            _commitBarMaskLeft.style.height = Length.Percent(fill * 100f);
            _commitBarMaskRight.style.height = Length.Percent(fill * 100f);
        }

        private void HideEarlyCommitment() => _earlyCommitmentContainer.style.display = DisplayStyle.None;

    }
}
