using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Nodes
{
    public static void AddChild<N>(this Node parent, PackedScene scene, Action<N> callback)
        where N : Node
    {
        var child = (N)scene.Instantiate();

        child.Ready += () => callback(child);

        parent.AddChild(child);
    }

    /// <summary>
    /// Iterates over all descendants depth-first. Each set of siblings is passed to
    /// the selector with their common parent.
    /// 
    /// Note that the last few calls to the selector will include 0 children.
    /// </summary>
    /// <typeparam name="T">The type returned from the selector; can be anything.</typeparam>
    /// <param name="n">The starting node, which will be the first argument
    /// for the first call to the selector.</param>
    /// <param name="selector">A function to transform "families" of nodes
    /// into an arbitrary datatype.</param>
    /// <returns>All values returned by the selector.</returns>
    public static IEnumerable<T> GetDescendantsDepthFirst<T>(this Node n, Func<Node, IEnumerable<Node>, T> selector)
    {
        var children = n.GetChildren();

        yield return selector(n, children);

        foreach (var result in children
            .SelectMany(c => c.GetDescendantsDepthFirst(selector)))
        {
            yield return result;
        }
    }

    /// <summary>
    /// Iterates over all descendants breadth-first. Each level of descendants is
    /// transformed into a single result, without regard for parent-child
    /// relationships.
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="selector"/>; can be anything.</typeparam>
    /// <param name="n">The starting node, which will not be passed to <paramref name="selector"/>.</param>
    /// <param name="selector">A function to transform generations of nodes
    /// into an arbitrary datatype.</param>
    /// <returns>All values returned by <paramref name="selector"/>.</returns>
    public static IEnumerable<T> GetDescendantsBreadthFirst<T>(this Node n, Func<IEnumerable<Node>, T> selector)
    {
        Node[] descendants = [n];

        while (true)
        {
            descendants = descendants
                .SelectMany(c => c.GetChildren())
                .ToArray();

            // By putting the condition here we don't call 'selector([n])'
            // and don't yield an empty generation.
            if (descendants.Length == 0)
            {
                break;
            }

            yield return selector(descendants);
        }
    }

    /// <summary>
    /// Iterates over all children of the node, transforming
    /// them with <paramref name="selector"/>.
    /// </summary>
    /// <param name="n">A node which may or may not have children.</param>
    /// <returns>All values returned by <paramref name="selector"/>.</returns>
    public static IEnumerable<T> GetChildren<T>(this Node n, Func<Node, T> selector)
        => n.GetChildren()
            .Select(selector);

    /// <summary>
    /// Iterates over all children of the node and their indexes, transforming
    /// them with <paramref name="selector"/>.
    /// </summary>
    /// <param name="n">A node which may or may not have children.</param>
    /// <returns>All values returned by <paramref name="selector"/>.</returns>
    public static IEnumerable<T> GetChildren<T>(this Node n, Func<Node, int, T> selector)
        => n.GetChildren()
            .Select(selector);
}
