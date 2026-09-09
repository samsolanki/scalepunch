using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScalePunch.Player;

namespace ScalePunch.UI
{
    /// <summary>
    /// Cycles the weapon's target priority.
    ///
    /// This belongs on the HUD, not in a settings menu. With automatic fire from
    /// a fixed position it is one of the very few tactical decisions a run
    /// offers, and burying it wastes it (docs/01-game-design.md §2.1).
    /// </summary>
    public class TargetPriorityToggle : MonoBehaviour
    {
        [SerializeField] AutoShoot weapon;
        [SerializeField] Button button;
        [SerializeField] TMP_Text label;

        static readonly AutoShoot.TargetPriority[] Order =
        {
            AutoShoot.TargetPriority.Closest,
            AutoShoot.TargetPriority.MostAdvanced,
            AutoShoot.TargetPriority.Toughest,
            AutoShoot.TargetPriority.Weakest
        };

        static readonly string[] Labels = { "Closest", "Advanced", "Toughest", "Weakest" };

        int _index;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Cycle);

            _index = weapon != null ? Mathf.Max(0, System.Array.IndexOf(Order, weapon.Priority)) : 0;
            Refresh();
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Cycle);
        }

        void Cycle()
        {
            _index = (_index + 1) % Order.Length;
            if (weapon != null) weapon.Priority = Order[_index];
            Refresh();
        }

        void Refresh()
        {
            if (label != null) label.text = Labels[_index];
        }
    }
}
