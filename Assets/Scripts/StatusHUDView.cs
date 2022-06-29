using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBuilder
{
    public class StatusHUDView : HUDViewBase
    {
        [SerializeField]
        private Text _hpText;

        [SerializeField]
        private Text _powerText;

        public void SetHP(int hp)
        {
            _hpText.text = hp.ToString();
        }

        public void SetPower(int power)
        {
            _powerText.text = power.ToString();
        }
    }
}
