using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Julo.Panels
{

    public class PanelManager : MonoBehaviour {

        public Panel initiallyOpen;

        protected Panel current;

        private GameObject previouslySelected;

        private const string isOpenParameterName = "IsOpen";
        private int isOpenParameterId;

        private const string openStateName = "Open";
        private const string closedStateName = "Closed";

        //public void OnEnable()
        private void Start()
        {
            //We cache the Hash to the "Open" Parameter, so we can feed to Animator.SetBool.
            isOpenParameterId = Animator.StringToHash(isOpenParameterName);

            /* hide all panels
            foreach(Panel panel in JuloFind.allDescendants<Panel>(this))
            {
                if(panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                }
            }
            */

            if(initiallyOpen != null)
            {
                OpenPanel(initiallyOpen);
            }
        }

        public bool OpenPanel(Panel panelToOpen)
        {
            if(panelToOpen == current)
            {
                return false;
            }

            // activate this panel
            panelToOpen.gameObject.SetActive(true);

            // Save the currently selected button that was used to open this Screen. (CloseCurrent will modify it)
            GameObject newPreviouslySelected = EventSystem.current.currentSelectedGameObject;

            // move the panel to front
            panelToOpen.transform.SetAsLastSibling();

            CloseCurrent();

            previouslySelected = newPreviouslySelected;

            current = panelToOpen;

            if(current.animator)
            {
                current.animator.SetBool(isOpenParameterId, true);
            }

            GameObject newSelected = FindFirstEnabledSelectable(panelToOpen.gameObject);

            SetSelected(newSelected);

            return true;
        }

        private GameObject FindFirstEnabledSelectable(GameObject container)
        {
            GameObject ret = null;

            var selectables = container.GetComponentsInChildren<Selectable>(true);

            foreach(var selec in selectables)
            {
                if(selec.IsActive() && selec.IsInteractable())
                {
                    ret = selec.gameObject;
                    break;
                }
            }

            return ret;
        }

        public void CloseCurrent()
        {
            if(current == null)
            {
                return;
            }

            //start the close animation...
            if(current.animator)
            {
                current.animator.SetBool(isOpenParameterId, false);
            }

            SetSelected(previouslySelected);

            Panel panelToClose = current;

            StartCoroutine(DisablePanelDelayed(panelToClose));

            current = null;

            // is delayed
            //panelToClose.gameObject.SetActive(false);
        }

        private IEnumerator DisablePanelDelayed(Panel panelToClose)
        {
            Animator anim = panelToClose.animator;

            bool closedStateReached = (anim == null);
            bool wantToClose = true;

            while(!closedStateReached && wantToClose)
            {
                if(!anim.IsInTransition(0))
                {
                    closedStateReached = anim.GetCurrentAnimatorStateInfo(0).IsName(closedStateName);
                }

                wantToClose = !anim.GetBool(isOpenParameterName);
                yield return new WaitForEndOfFrame();
            }

            if(wantToClose)
            {
                panelToClose.gameObject.SetActive(false);
            }
        }

        private void SetSelected(GameObject newSelected)
        {
            EventSystem.current.SetSelectedGameObject(newSelected);

            var inputModule = EventSystem.current.currentInputModule as StandaloneInputModule;

            if(inputModule == null)
            { // is a pointer device
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    } // class PanelManager

} // namespace Julo.Panels
