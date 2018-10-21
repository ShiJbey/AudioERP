using UnityEngine;
using UnityEngine.Events;

namespace AudioERP
{
    /*
     * Menu options have an associated audio clip which is
     * played when the option is being used to elicit an ERP
     * event.
     */
    public class ERPMenuOption : MonoBehaviour
    { 
        AudioSource audioClip;
        public bool selected { get; set; }
        public UnityEvent selectionEvent;


        // Use this for initialization
        void Start()
        {
            this.audioClip = GetComponent<AudioSource>();
            if (selectionEvent == null)
            {
                selectionEvent = new UnityEvent();
            }
        }

        // Toggles the halo component attached to the option
        public void VisualHighlight(bool on)
        {
            UnityEngine.Behaviour h = (Behaviour)this.GetComponent("Halo");
            if (h != null)
            {
                if (on)
                {
                    h.enabled = true;
                }
                else
                {
                    h.enabled = false;
                }
            }
        }

        public void Play()
        {
            Debug.Log(this.gameObject.name +" is Playing sound");
            audioClip.Play();
        }

        // Highlighting the menu option causes the audio clip to
        // play
        public void OnHighlight()
        {
            audioClip.Play();
        }

        public void Select()
        {
            this.selected = true;
        }

        public void Deselect()
        {
            this.selected = false;
        }

        public void OnSelect()
        {
            // This should call a delegate function that can be set in the editor.
            selectionEvent.Invoke();
        }
    }
}
