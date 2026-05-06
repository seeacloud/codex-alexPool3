using TMPro;
using UnityEngine;

namespace PoolAimTrainer.UI
{
    public class HintPanel : MonoBehaviour
    {
        public TMP_Text text;

        public void Show(string s)
        {
            if (text != null) text.text = s;
        }

        public void Clear()
        {
            if (text != null) text.text = "";
        }
    }
}
