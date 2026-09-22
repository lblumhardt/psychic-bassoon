using UnityEngine;

public static class CombatVfxBurst
{
    private static Material _playerMarker;
    private static Material _enemyMarker;
    private static Material _particleMaterial;

    public static void Spawn(Vector3 position, Color color, int count, float speed, float lifetime)
    {
        GameObject effect = new GameObject("Combat Particles");
        effect.transform.position = position;
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = Mathf.Max(0.05f, lifetime);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.35f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colors = particles.colorOverLifetime;
        colors.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.7f, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) });
        colors.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = ParticleMaterial();
        particles.Emit(Mathf.Max(1, count));
        particles.Play();
        Object.Destroy(effect, lifetime + 0.3f);
    }

    public static Material TeamMarkerMaterial(Color color)
    {
        bool player = color.b > color.r;
        Material cached = player ? _playerMarker : _enemyMarker;
        if (cached != null) return cached;
        Material material = CombatVisuals.MakeMaterial(new Color(color.r, color.g, color.b, 0.38f));
        if (player) _playerMarker = material;
        else _enemyMarker = material;
        return material;
    }

    private static Material ParticleMaterial()
    {
        if (_particleMaterial != null) return _particleMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Sprites/Default");
        _particleMaterial = new Material(shader);
        _particleMaterial.color = Color.white;
        return _particleMaterial;
    }
}
