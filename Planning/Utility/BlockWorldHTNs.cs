using BoltFreezer.DecompTools;
using BoltFreezer.Interfaces;
using BoltFreezer.PlanTools;
using BoltFreezer.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockWorldHTNs  {

    public static Decomposition MultiMove()
    {
        // Params
        var objTerms = new List<ITerm>() {
                new Term("?agent")     { Type = "steeringagent"}, //0
                new Term("?from")       { Type = "location"}, //1
                new Term("?to")         { Type = "location"}, //2
                new Term("?intermediate") {Type = "location"} //3
            };

        var litTerms = new List<IPredicate>();

        var atAgentOrigin = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[1] }, true);
        var atAgentInt = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[3] }, true);
        var atAgentDest = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[2] }, true);

        var move1 = new PlanStep(new Operator("",
           new List<IPredicate>() { atAgentOrigin },
           new List<IPredicate> { atAgentInt }));

        var move2 = new PlanStep(new Operator("",
           new List<IPredicate>() { atAgentInt },
           new List<IPredicate> { atAgentDest }));

        var substeps = new List<IPlanStep>() { move1, move2 };

        var sublinks = new List<CausalLink<IPlanStep>>()
            {
                new CausalLink<IPlanStep>(atAgentInt, move1, move2)
            };

        var suborderings = new List<Tuple<IPlanStep, IPlanStep>>()
            {
                new Tuple<IPlanStep,IPlanStep>(move1, move2)
            };

        var root = new Operator(new Predicate("multimove", objTerms, true));
        var decomp = new Decomposition(root, litTerms, substeps, suborderings, sublinks)
        {
            NonEqualities = new List<List<ITerm>>() {
                        new List<ITerm>() { objTerms[1], objTerms[2] },
                        new List<ITerm>() { objTerms[2], objTerms[3] },
                        new List<ITerm>() { objTerms[1], objTerms[3] }
                }
        };

        return decomp;
    }

    public static Decomposition Transport()
    {

        // Params
        var objTerms = new List<ITerm>() {
                new Term("?agent")     { Type = "steeringagent"}, //0
                new Term("?item")        { Type = "block"}, //1
                new Term("?from")       { Type = "location"}, //2
                new Term("?to")         { Type = "location"}, //3
                new Term("?adjacentfrom") {Type = "location"}, //4
                new Term("?adjacentto") {Type = "location"} //5
            };

        var litTerms = new List<IPredicate>();
        // pickup (?taker - agent ?block - block ?location - location ?takerLocation - location)
        var pickupterms = new List<ITerm>() { objTerms[0], objTerms[1], objTerms[2], objTerms[4] };

        // This guaranteed move
        //var moveterms = new List<ITerm>() { objTerms[0], objTerms[4], objTerms[5] };

        //(?putter - agent ?thing - block ?agentlocation - location ?newlocation - location)
        var putdownterms = new List<ITerm>() { objTerms[0], objTerms[1], objTerms[5], objTerms[3] };

        var pickup = new PlanStep(new Operator(new Predicate("pickup", pickupterms, true)));
        // var travelOp = new Operator("", new List<IPredicate>(), new List<IPredicate>(){ atPersonTo});

        var putdown = new PlanStep(new Operator(new Predicate("putdown", putdownterms, true)));

        var atAgentOrigin = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[4] }, true);
        var hasAgentThing = new Predicate("has", new List<ITerm>() { objTerms[0], objTerms[1] }, true);
        var atAgentDest = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[5] }, true);

        var move = new PlanStep(new Operator("",
           new List<IPredicate>() { atAgentOrigin },
           new List<IPredicate> { atAgentDest }));

        //new Operator()
        var substeps = new List<IPlanStep>() { pickup, move, putdown };
        var sublinks = new List<CausalLink<IPlanStep>>()
            {
               // new CausalLink<IPlanStep>(atAgentOrigin, pickup, move),
                new CausalLink<IPlanStep>(hasAgentThing, pickup, putdown),
                new CausalLink<IPlanStep>(atAgentDest, move, putdown),
            };
        var suborderings = new List<Tuple<IPlanStep, IPlanStep>>()
            {
                new Tuple<IPlanStep,IPlanStep>(pickup, move),
                new Tuple<IPlanStep,IPlanStep>(move, putdown)
            };

        var root = new Operator(new Predicate("transport", objTerms, true));
        var decomp = new Decomposition(root, litTerms, substeps, suborderings, sublinks)
        {
            NonEqualities = new List<List<ITerm>>() {
                        new List<ITerm>() { objTerms[2], objTerms[3] },
                        new List<ITerm>() { objTerms[2], objTerms[4] },
                        new List<ITerm>() { objTerms[3], objTerms[5] }
                }
        };

        return decomp;
    }

    public static Composite ReadMultimoveCompositeOperator()
    {
        var objTerms = new List<ITerm>() {
                new Term("?agent")     { Type = "steeringagent"},
                new Term("?from")         { Type = "location"},
                new Term("?to")         { Type = "location"},
            };

        var atAgentFrom = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[1] }, true);
        var atAgentTo = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[2] }, true);

        var op =
            new Operator(new Predicate("Multimove", objTerms, true),
                new List<IPredicate>() { atAgentFrom },
                new List<IPredicate>() { atAgentTo, atAgentFrom.GetReversed() }
            )
            {
                NonEqualities = new List<List<ITerm>>() {
                        new List<ITerm>() { objTerms[1], objTerms[2] }
                }
            };

        return new Composite(op);
    }

    public static Composite ReadTransportCompositeOperator()
    {
        var objTerms = new List<ITerm>() {
                new Term("?agent")     { Type = "steeringagent"}, //0
                new Term("?item")         { Type = "block"}, //1
                new Term("?adjacentfrom") {Type="location"}, //2
                new Term("?from")         { Type = "location"}, //3
                new Term("?to")         { Type = "location"} //4
            };

        var atItemFrom = new Predicate("at", new List<ITerm>() { objTerms[1], objTerms[3] }, true);
        var atAgentFrom = new Predicate("at", new List<ITerm>() { objTerms[0], objTerms[2] }, true);
        var atItemTo = new Predicate("at", new List<ITerm>() { objTerms[1], objTerms[4] }, true);
        var freeHands = new Predicate("freehands", new List<ITerm>() { objTerms[0] }, true);
        var occupied = new Predicate("occupied", new List<ITerm>() { objTerms[4] }, true);
        var adjacent = new Predicate("adjacent", new List<ITerm>() { objTerms[2], objTerms[3] }, true);

        var op =
            new Operator(new Predicate("Transport", objTerms, true),
                new List<IPredicate>() { atItemFrom, atAgentFrom, adjacent, freeHands, occupied.GetReversed() },
                new List<IPredicate>() { atItemTo }
            //freeHands, occupied, atItemFrom.GetReversed()}
            )
            {
                NonEqualities = new List<List<ITerm>>() {
                        new List<ITerm>() { objTerms[3], objTerms[4] },
                        new List<ITerm>(){objTerms[2], objTerms[3]},
                        new List<ITerm>(){objTerms[3], objTerms[4]}
                }
            };

        return new Composite(op);
    }

    public static List<Decomposition> ReadTransportDecompositions()
    {
        var decomps = new List<Decomposition>();

        var transport = Transport();
        decomps.Add(transport);
        return decomps;
    }

    public static List<Decomposition> ReadMultimoveDecompositions()
    {
        var decomps = new List<Decomposition>();
        var multimove = MultiMove();
        decomps.Add(multimove);
        return decomps;
    }

    public static Tuple<Composite, List<Decomposition>> TransportComposites()
    {
        var decomps = ReadTransportDecompositions();
        var composite = ReadTransportCompositeOperator();
        var CompositeMethods = new Tuple<Composite, List<Decomposition>>(composite, decomps);
        return CompositeMethods;
    }

    public static Tuple<Composite, List<Decomposition>> MultimoveComposites()
    {
        var decomps = ReadMultimoveDecompositions();
        var composite = ReadMultimoveCompositeOperator();
        var CompositeMethods = new Tuple<Composite, List<Decomposition>>(composite, decomps);
        return CompositeMethods;
    }

    public static Dictionary<Composite, List<Decomposition>> ReadCompositeOperators()
    {
        var compositeDecompList = new Dictionary<Composite, List<Decomposition>>();
        var transport = TransportComposites();
        compositeDecompList[transport.First] = transport.Second;//.Add(transport);
        var multimove = MultimoveComposites();
        compositeDecompList[multimove.First] = multimove.Second;
        return compositeDecompList;
    }
}
