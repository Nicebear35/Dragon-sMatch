using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.Events;

public class AnimationObserver
{
    private int _tweenCounter;

    public event UnityAction AllAnimationsEnded;

    public AnimationObserver()
    {
        _tweenCounter = 0;
    }
    public void ObserveTweenProgress(Tween tween)
    {
        tween.onComplete += TweenFinishDetector;
        _tweenCounter++;
    }

    private void TweenFinishDetector()
    {
        _tweenCounter--;

        if (_tweenCounter == 0)
        {
            AllAnimationsEnded.Invoke();
        }
    }
}
