using System;
using System.Collections.Generic;
using MeridianClient.Models;

namespace MeridianClient.Services;

public class GraphSimulationEngine
{
    public double RepulsionConstant { get; set; } = 5500.0;
    public double SpringConstant { get; set; } = 0.045;
    public double DesiredEdgeDistance { get; set; } = 120.0;
    public double GravityConstant { get; set; } = 0.015;
    public double Damping { get; set; } = 0.85;
    public double MaxVelocity { get; set; } = 14.0;
    public double EnergyThreshold { get; set; } = 0.05;

    public bool IsSleeping { get; private set; }

    public void WakeUp()
    {
        IsSleeping = false;
    }

    public double Step(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyList<GraphEdge> edges,
        double centerX,
        double centerY,
        double dt = 0.8)
    {
        if (IsSleeping || nodes.Count == 0)
        {
            return 0;
        }

        int count = nodes.Count;

        // 1. Repulsion between all pairs of nodes (Coulomb)
        for (int i = 0; i < count; i++)
        {
            var nodeA = nodes[i];

            for (int j = i + 1; j < count; j++)
            {
                var nodeB = nodes[j];

                double dx = nodeA.X - nodeB.X;
                double dy = nodeA.Y - nodeB.Y;
                double distSq = dx * dx + dy * dy;

                if (distSq < 1.0)
                {
                    // Avoid singularity if nodes coincide
                    dx = (i % 2 == 0 ? 1 : -1) * 2.0;
                    dy = (j % 2 == 0 ? 1 : -1) * 2.0;
                    distSq = dx * dx + dy * dy;
                }

                double dist = Math.Sqrt(distSq);
                if (dist > 650.0) continue; // Cut off distant nodes for speed

                double minDistance = nodeA.Radius + nodeB.Radius + 15.0;
                double force = RepulsionConstant / (distSq + 20.0);
                if (dist < minDistance)
                {
                    // Strong pushback if overlapping
                    force += (minDistance - dist) * 1.2;
                }

                double fx = (dx / dist) * force;
                double fy = (dy / dist) * force;

                nodeA.Vx += fx / nodeA.Mass;
                nodeA.Vy += fy / nodeA.Mass;
                nodeB.Vx -= fx / nodeB.Mass;
                nodeB.Vy -= fy / nodeB.Mass;
            }
        }

        // 2. Spring attraction along edges (Hooke)
        int edgeCount = edges.Count;
        for (int i = 0; i < edgeCount; i++)
        {
            var edge = edges[i];
            var source = edge.Source;
            var target = edge.Target;

            double dx = target.X - source.X;
            double dy = target.Y - source.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < 1.0) dist = 1.0;

            double desired = DesiredEdgeDistance + (source.Radius + target.Radius);
            double delta = dist - desired;
            double force = delta * SpringConstant;

            double fx = (dx / dist) * force;
            double fy = (dy / dist) * force;

            source.Vx += fx;
            source.Vy += fy;
            target.Vx -= fx;
            target.Vy -= fy;
        }

        // 3. Gravity toward center + Damping + Integration
        double totalKineticEnergy = 0.0;

        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];

            if (node.IsPinned)
            {
                node.Vx = 0;
                node.Vy = 0;
                continue;
            }

            // Pull gently to center
            double gx = (centerX - node.X) * GravityConstant;
            double gy = (centerY - node.Y) * GravityConstant;
            node.Vx += gx;
            node.Vy += gy;

            // Damping / Friction
            node.Vx *= Damping;
            node.Vy *= Damping;

            // Clamp velocity
            double speed = Math.Sqrt(node.Vx * node.Vx + node.Vy * node.Vy);
            if (speed > MaxVelocity)
            {
                node.Vx = (node.Vx / speed) * MaxVelocity;
                node.Vy = (node.Vy / speed) * MaxVelocity;
            }

            // Move
            node.X += node.Vx * dt;
            node.Y += node.Vy * dt;

            totalKineticEnergy += (node.Vx * node.Vx + node.Vy * node.Vy);
        }

        // Sleep when stable to conserve CPU
        if (totalKineticEnergy < EnergyThreshold)
        {
            IsSleeping = true;
        }

        return totalKineticEnergy;
    }
}
