using UnityEngine;

namespace Julo.Panels
{

    public class Panel : MonoBehaviour {


        bool _searchedAnimator = false;
        Animator _animator = null;

        public Animator animator
        {
            get {
                if(!_searchedAnimator)
                {
                    _animator = GetComponent<Animator>();
                    _searchedAnimator = true;
                }

                return _animator;
            }
        }

    } // class Panel

} // namespace Julo.Panels

