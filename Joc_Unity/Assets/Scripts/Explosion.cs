using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public AnimatedSpriteRenderer start;
    public AnimatedSpriteRenderer middle;
    public AnimatedSpriteRenderer end;

    public void SetActiveRenderer(AnimatedSpriteRenderer renderer)
    {
        start.enabled  = (renderer == start);
        middle.enabled = (renderer == middle);
        end.enabled    = (renderer == end);
    }

    public void SetDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void DestroyAfter(float duration)
    {
        StartCoroutine(DestroyAfterRoutine(duration));
    }

    private IEnumerator DestroyAfterRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
