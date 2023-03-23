using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParabolaCalculator : MonoBehaviour
{
    public float strength = 0.981f;
    public Vector3 down = Vector3.down;

    Vector3 p;
    Vector3 v;
    float tDelta;

    public Vector3 init(Vector3 p0, Vector3 v0, float timeStepSize) {
        p = p0;
        v = v0;
        tDelta = timeStepSize;

        return p;
    }

    public Vector3 next() {
        p += v * tDelta;
        v += down * strength * tDelta;

        return p;
    }

    public List<Vector3> BezierArc(Transform origin, Transform target, int segments) {
        return BezierArc(origin, target, segments, Vector3.forward, Vector3.up);
    }

    public List<Vector3> BezierArc(Transform origin, Transform target, int segments, Vector3 originExitDir, Vector3 targetExitDir) {
        Vector3 orDir = (origin.rotation * originExitDir).normalized;
        Vector3 taDir = (target.rotation * targetExitDir).normalized;
        float dist = (origin.position - target.position).magnitude;

        Vector3 p0 = origin.position;
        Vector3 p1 = origin.position + orDir * dist / 3f;
        Vector3 p2 = target.position + taDir * dist / 3f;
        Vector3 p3 = target.position;

        return BezierArc(p0, p1, p2, p3, segments);
    }

    public List<Vector3> BezierArc(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segments) {
        List<Vector3> pts = new List<Vector3>();

        for(int i = 0; i <= segments; i++) {
            float t = ((float)i) / (segments);
            float it = 1f-t;
            pts.Add(
                1f * p0 * it * it * it +
                3f * p1 * it * it *  t +
                3f * p2 * it *  t *  t +
                1f * p3 *  t *  t *  t
            );
        }

        return pts;
    }

    /*public ArrayList calcParabola(Vector3 p0, Vector3 v0, float tDelta, float maxDist = 20, int maxIterations = 100) {
        Vector3 p = p0;
        Vector3 v = v0;
        float dist = 0;

        ArrayList ret = new ArrayList();
        ret.Add(p);

        while(dist < maxDist && maxIterations-- > 0) {
            v += down * strength * tDelta;
            p += v * tDelta;
            dist += v.magnitude * tDelta;

            if(v.magnitude == 0 && (down * strength).magnitude == 0) {
                break;
            }
        }

        return ret;
    }*/
}
