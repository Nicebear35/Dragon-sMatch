using DG.Tweening;
using UnityEngine;

public class CellView : MonoBehaviour
{
    private const float ScalingUpTime = 0.05f;
    private const float ScalingDownTime = 0.15f;
    private const float MovingTime = 0.5f;

    public void Delete()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(transform.localScale * 1.1f, ScalingDownTime));
        sequence.Append(transform.DOScale(Vector3.zero, ScalingUpTime));
        sequence.Play();

        Destroy(gameObject, ScalingDownTime + ScalingDownTime);
    }

    public Tween TakePlace(Vector3 position)
    {
        return transform.DOMove(position, MovingTime);
    }
}
