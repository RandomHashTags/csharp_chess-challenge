///
/// Created by Evan Anderson on August 18, 2023.
///

using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

// testing
using System.Diagnostics;

/// LAST BRAIN POWER: 648
/// 
/// notes: `ToList` and `Union` functions save brain power :)
public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        KilobyteMove[] best_moves = get_best_moves(board);
        KilobyteMove best_move = best_moves[0];
        if (best_move.move_type == KilobyteMoveType.unknown)
        {
            best_move = best_moves[new Random().Next(best_moves.Length)];
        }
        //Debug.WriteLine("play=" + board.PlyCount + ";best_move=" + best_move.move.ToString() + ";best_move_type=" + best_move.move_type.ToString());
        return best_move.move;
    }

    KilobyteMove[] get_best_moves(Board board)
    {
        return collect_moves(board, false, KilobyteMoveType.check_mate, move => true, true, move => board.IsInCheckmate())
            .Union(collect_moves(board, false, KilobyteMoveType.check_cant_take, move => true, true, move => board.IsInCheck() && get_opponent_attacking_moves(board).Where(test => test.move.TargetSquare == move.TargetSquare).ToArray().Length == 0))
            .Union(collect_moves(board, false, KilobyteMoveType.promotion, move => true, true, move => move.IsPromotion))
            .Union(collect_moves(board, true, KilobyteMoveType.capture_prevent_promotion_cant_take, move =>
            {
                board.ForceSkipTurn();
                bool value = collect_moves(board, false, KilobyteMoveType.promotion, model => true, true, move => move.IsPromotion).ToArray().Length != 0;
                board.UndoSkipTurn();
                return value;
            }, true, move => get_opponent_attacking_moves(board).Where(test => test.move.TargetSquare == move.TargetSquare).ToArray().Length == 0))
            .Union(get_best_capture_moves(board))
            .Union(get_best_defensive_moves(board))
            .Union(collect_moves(board, false, KilobyteMoveType.castle, model => true, true, move => move.IsCastles))
            .Union(board.GetLegalMoves().Select(move => new KilobyteMove(move, KilobyteMoveType.unknown)))
        .ToArray();
    }

    Func<Board, bool, KilobyteMoveType, Func<Move, bool>, bool, Func<Move, bool>, HashSet<KilobyteMove>> collect_moves = (board, capture_only, move_type, precondition, make_move, transform) =>
    {
        return board.GetLegalMoves(capture_only).Where(move =>
        {
            if(!precondition(move))
            {
                return false;
            }
            if(make_move) {
                board.MakeMove(move);
                bool value = transform(move);
                board.UndoMove(move);
                return value;
            } else
            {
                board.ForceSkipTurn();
                bool value = transform(move);
                board.UndoSkipTurn();
                return value;
            }
        })
            .Select(move => new KilobyteMove(move, move_type))
            .ToHashSet();
    };

    HashSet<KilobyteMove> get_best_capture_moves(Board board)
    {
        return board.GetLegalMoves(true).Where(capture_move =>
        {
            Move[] opponent_captures = get_opponent_capturing_moves(board, capture_move);
            return opponent_captures.Length == 0 // capture an undefended piece
            || opponent_captures.Where(opponent_capture => (int) opponent_capture.CapturePieceType <= (int) capture_move.CapturePieceType).ToArray().Length != 0 // trade same piece OR a lower tier piece for a higher tier one
            ;
        })
            .Select(move => new KilobyteMove(move, KilobyteMoveType.capture))
            .ToHashSet();
    }

    HashSet<KilobyteMove> get_best_defensive_moves(Board board)
    {
        HashSet<KilobyteMove> best_moves = new HashSet<KilobyteMove>();
        foreach(KilobyteMove attack in get_opponent_attacking_moves(board))
        {
            best_moves.Union(collect_moves(board, false, KilobyteMoveType.defend, move => true, false, move => move.TargetSquare == attack.move.TargetSquare));
        }
        return best_moves;
    }
    Move[] get_opponent_capturing_moves(Board board, Move after_move)
    {
        board.MakeMove(after_move);
        Move[] moves = board.GetLegalMoves(true).Where(move => move.TargetSquare == after_move.TargetSquare).ToArray();
        board.UndoMove(after_move);
        return moves;
    }
    KilobyteMove[] get_opponent_attacking_moves(Board board)
    {
        return collect_moves(board, true, KilobyteMoveType.capture, move => true, false, move => true).ToArray();
    }

    struct KilobyteMove
    {
        public Move move;
        public KilobyteMoveType move_type;

        public KilobyteMove(Move move, KilobyteMoveType move_type)
        {
            this.move = move;
            this.move_type = move_type;
        }
    }
    enum KilobyteMoveType
    {
        check_mate,
        check_cant_take,
        get_out_of_check,
        capture_prevent_promotion_cant_take,
        promotion,
        defend,
        capture_cant_trade,
        castle,
        capture,
        unknown
    }
}