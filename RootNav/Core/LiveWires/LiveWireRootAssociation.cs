using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RootNav.Core.LiveWires
{
    public static class LiveWireRootAssociation
    {
        public static void FindRoots(RootTerminalCollection terminalCollection, List<LiveWirePrimaryPath> liveWirePaths, List<LiveWireWeightDescriptor> liveWirePathWeights, out List<LiveWirePrimaryPath> finalPaths, out List<LiveWireWeightDescriptor> finalWeights)
        {
            // Arrays for indexing
            int sourceCount = terminalCollection.Sources.Count();
            int tipCount = terminalCollection.Primaries.Count();
            Dictionary<Tuple<int, int>, double> pathWeights = new Dictionary<Tuple<int, int>, double>();


            // Create association array:
            Dictionary<int, int?> rootAssociations = new Dictionary<int, int?>();
            for (int tipIndex = 0; tipIndex < terminalCollection.Count; tipIndex++)
            {
                if (terminalCollection[tipIndex].Type == TerminalType.Primary)
                {
                    bool found = false;
                    foreach (var pair in terminalCollection.TerminalLinks)
                    {
                        if (pair.Item2 == terminalCollection[tipIndex])
                        {
                            found = true;
                            rootAssociations.Add(tipIndex, terminalCollection.IndexOf(pair.Item1));
                            break;
                        }
                    }

                    if (!found)
                    {
                        rootAssociations.Add(tipIndex, null);
                    }
                }
            }

            // Currently all associations are null, no roots have been found.

            // for each tip
            for (int tipIndex = 0; tipIndex < terminalCollection.Count; tipIndex++)
            {
                if (terminalCollection[tipIndex].Type != TerminalType.Primary || rootAssociations[tipIndex] != null) { continue; }

                // Calculate weight of path to each root
                int finalIndex = 0;
                double finalWeight = double.MinValue;
                // For each unlinked source
                for (int sourceIndex = 0; sourceIndex < terminalCollection.Count; sourceIndex++)
                {
                    if (terminalCollection[sourceIndex].Type != TerminalType.Source || !terminalCollection.UnlinkedSources.Contains(terminalCollection[sourceIndex])) { continue; }

                    // Find path index
                    int pathIndex = 0;
                    foreach (LiveWirePrimaryPath path in liveWirePaths)
                    {
                        if (tipIndex == path.TipIndex && sourceIndex == path.SourceIndex)
                            break;
                        pathIndex++;
                    }

                    // Obtain weight
                    double pathWeight = LiveWirePathWeights.CalculateTotalWeight(liveWirePathWeights[pathIndex]);

                    pathWeights.Add(new Tuple<int, int>(tipIndex, sourceIndex), pathWeight);

                    if (finalWeight < pathWeight)
                    {
                        finalWeight = pathWeight;
                        finalIndex = sourceIndex;
                    }
                }

                // Assign highest weight to association
                rootAssociations[tipIndex] = finalIndex;
            }


            // For each unassociated source, find the most appropriate root and reroute it there, but only if this root is sharing a source with another.
            // -> This prevents sources stealing tips from other sources.

            // If sources <= tips, and any sources remain, then two must have been assigned to the same source where this was not appropriate.
            if (sourceCount <= tipCount)
            {
                // There is not more sources than tips, this means any empty source must be connected to by a root.
                List<int> emptySourceList = new List<int>();

                // Find all empty sources
                foreach (RootTerminal source in terminalCollection.Sources)
                {
                    int index = terminalCollection.IndexOf(source);

                    bool referenceFound = false;
                    foreach (int? reference in rootAssociations.Values)
                    {
                        if (reference.HasValue && reference.Value == index)
                        {
                            referenceFound = true;
                        }
                    }

                    if (!referenceFound)
                    {
                        emptySourceList.Add(index);
                    }
                }

                // For each empty source, find an alternative that is the best match that has two tips going to it
                foreach (int sourceIndex in emptySourceList)
                {
                    List<int> possibleTips = new List<int>();

                    // For each tip
                    foreach (int i in rootAssociations.Keys)
                    {
                        if (rootAssociations[i].HasValue)
                        {
                            int source = rootAssociations[i].Value; // Source linked to this tip
                            int occurenceCount = rootAssociations.Count(p => p.Value == source);
                            // The occurence count is how often a source has occured
                            if (occurenceCount > 1)
                            {
                                possibleTips.Add(i);
                            }
                        }

                    }

                    int finalIndex = 0;
                    double finalWeight = double.MinValue;
                    foreach (int tipIndex in possibleTips)
                    {
                        double weight = pathWeights[new Tuple<int, int>(tipIndex, sourceIndex)];
                        if (finalWeight < weight)
                        {
                            finalWeight = weight;
                            finalIndex = tipIndex;
                        }
                    }

                    rootAssociations[finalIndex] = sourceIndex;

                }
            }

            // Use assocations to create new list
            finalPaths = new List<LiveWirePrimaryPath>();
            finalWeights = new List<LiveWireWeightDescriptor>();

            foreach (KeyValuePair<int, int?> kvp in rootAssociations)
            {
                int tipIndex = kvp.Key;
                int sourceIndex = 0;
                if (kvp.Value.HasValue)
                    sourceIndex = kvp.Value.Value;
                else
                    continue; // This tip has no source associated with it. Perhaps there are too many tips.

                // Find path index
                int pathIndex = 0;
                foreach (LiveWirePrimaryPath path in liveWirePaths)
                {
                    if (tipIndex == path.TipIndex && sourceIndex == path.SourceIndex)
                    {
                        finalPaths.Add(liveWirePaths[pathIndex]);
                        finalWeights.Add(liveWirePathWeights[pathIndex]);
                        break;
                    }
                    pathIndex++;
                }



            }
        }
    }
}
