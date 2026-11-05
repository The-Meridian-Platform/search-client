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
    public int MaxIterations { get; set; } = 140;

    public bool IsSleeping { get; private set; }
    public int IterationCount { get; private set; }

    public event Action? SimulationStabilized;

    public void WakeUp()
    {
        IsSleeping = false;
        IterationCount = 0;
    }

    public void StepLocal(GraphNode draggedNode, IReadOnlyList<GraphEdge> edges, double dt = 0.8)
    {
        if (draggedNode == null) return;

        int edgeCount = edges.Count;
        for (int i = 0; i < edgeCount; i++)
        {
            var edge = edges[i];
            if (!edge.IsVisible) continue;
            if (edge.Source != draggedNode && edge.Target != draggedNode) continue;

            var neighbor = edge.Source == draggedNode ? edge.Target : edge.Source;
            if (!neighbor.IsVisible || neighbor.IsPinned) continue;

            double dx = neighbor.X - draggedNode.X;
            double dy = neighbor.Y - draggedNode.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1.0) dist = 1.0;

            double desired = DesiredEdgeDistance + (draggedNode.Radius + neighbor.Radius);
            double delta = dist - desired;
            double force = delta * SpringConstant * 0.4;

            double fx = (dx / dist) * force;
            double fy = (dy / dist) * force;

            neighbor.Vx -= fx;
            neighbor.Vy -= fy;
            neighbor.Vx *= Damping;
            neighbor.Vy *= Damping;

            neighbor.X += neighbor.Vx * dt;
            neighbor.Y += neighbor.Vy * dt;
        }
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

        IterationCount++;
        int count = nodes.Count;

        for (int i = 0; i < count; i++)
        {
            var nodeA = nodes[i];
            if (!nodeA.IsVisible) continue;

            for (int j = i + 1; j < count; j++)
            {
                var nodeB = nodes[j];
                if (!nodeB.IsVisible) continue;

                double dx = nodeA.X - nodeB.X;
                double dy = nodeA.Y - nodeB.Y;
                double distSq = dx * dx + dy * dy;

                if (distSq < 1.0)
                {
                    dx = (i % 2 == 0 ? 1 : -1) * 2.0;
                    dy = (j % 2 == 0 ? 1 : -1) * 2.0;
                    distSq = dx * dx + dy * dy;
                }

                double dist = Math.Sqrt(distSq);
                if (dist > 650.0) continue;

                double minDistance = nodeA.Radius + nodeB.Radius + 15.0;
                double force = RepulsionConstant / (distSq + 20.0);
                if (dist < minDistance)
                {
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

        int edgeCount = edges.Count;
        for (int i = 0; i < edgeCount; i++)
        {
            var edge = edges[i];
            if (!edge.IsVisible) continue;
            var source = edge.Source;
            var target = edge.Target;
            if (!source.IsVisible || !target.IsVisible) continue;

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

        double totalKineticEnergy = 0.0;

        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            if (!node.IsVisible) continue;

            if (node.IsPinned)
            {
                node.Vx = 0;
                node.Vy = 0;
                continue;
            }

            double gx = (centerX - node.X) * GravityConstant;
            double gy = (centerY - node.Y) * GravityConstant;
            node.Vx += gx;
            node.Vy += gy;

            node.Vx *= Damping;
            node.Vy *= Damping;

            double speed = Math.Sqrt(node.Vx * node.Vx + node.Vy * node.Vy);
            if (speed > MaxVelocity)
            {
                node.Vx = (node.Vx / speed) * MaxVelocity;
                node.Vy = (node.Vy / speed) * MaxVelocity;
            }

            node.X += node.Vx * dt;
            node.Y += node.Vy * dt;

            totalKineticEnergy += (node.Vx * node.Vx + node.Vy * node.Vy);
        }

        if (totalKineticEnergy < EnergyThreshold || IterationCount >= MaxIterations)
        {
            IsSleeping = true;
            SimulationStabilized?.Invoke();
        }

        return totalKineticEnergy;
    }
}
