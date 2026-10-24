using UnityEngine;

/// <summary>
/// Runtime-built 2D particle emitters for footsteps, dig debris, and gem sparkles.
/// </summary>
public static class GameplayFxParticles
{
    static Material particleMaterial;

    static Material SharedMaterial()
    {
        if (particleMaterial != null)
            return particleMaterial;
        var shader = Shader.Find("Sprites/Default");
        particleMaterial = shader != null ? new Material(shader) : null;
        return particleMaterial;
    }

    public static ParticleSystem CreateFootDust(Transform parent)
    {
        var go = new GameObject("FootDust");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 16;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.58f, 0.52f, 0.85f));
        main.gravityModifier = -0.15f;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = SharedMaterial();
        return ps;
    }

    public static ParticleSystem CreateDigDebris(Transform parent)
    {
        var go = new GameObject("DigDebris");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.28f, 0.18f, 1f));
        main.gravityModifier = 1.4f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = SharedMaterial();
        return ps;
    }

    public static ParticleSystem CreateGemSparkle(Transform parent)
    {
        var go = new GameObject("GemSparkle");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 12;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.55f, 0.9f));
        main.gravityModifier = 0.05f;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.04f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.35f), 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOver.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = SharedMaterial();
        return ps;
    }

    public static void EmitBurst(ParticleSystem ps, Vector3 worldPosition, int count)
    {
        if (ps == null)
            return;
        ps.transform.position = worldPosition;
        ps.Emit(count);
    }
}
