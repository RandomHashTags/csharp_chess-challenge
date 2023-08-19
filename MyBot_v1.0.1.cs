///
/// Created by Evan Anderson on August 19, 2023.
/// v1.0.1 | no machine learning, algorithm, or sophisticated logic
///

using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

// testing
using System.Diagnostics;
using System.Threading.Tasks;

/// LAST BRAIN POWER: 923 (without debug info | MAX ALLOWED: 1024)
/// 
/// notes: `ToList` and `Union` functions save brain power :)
public class MyBot : IChessBot
{
    Move[] opponent_captures;
    public Move Think(Board board, Timer timer)
    {
        opponent_captures = get_opponent_attacking_moves(board);
        KilobyteMove[] best_moves = get_best_moves(board);
        KilobyteMove best_move;
        if (best_moves.Length == 0 )
        {
            best_move = new KilobyteMove(board.GetLegalMoves()[new Random().Next(board.GetLegalMoves().Length)], KilobyteMoveType.unknown);
        } else if (best_moves[0].move_type == KilobyteMoveType.unknown)
        {
            best_move = best_moves[new Random().Next(best_moves.Length)];
        } else
        {
            best_move = best_moves[0];
        }
        //Debug.WriteLine("play=" + board.PlyCount + ";best_move=" + best_move.move.ToString() + ";best_move_type=" + best_move.move_type.ToString());
        return best_move.move;
    }

    KilobyteMove[] get_best_moves(Board board)
    {
        return collect_moves(board, false, KilobyteMoveType.check_mate, move => true, true, move => board.IsInCheckmate())
            .Union(collect_moves(board, false, KilobyteMoveType.check_cant_take, move => true, true, move => board.IsInCheck() && board.GetLegalMoves(true).Where(test => test.TargetSquare == move.TargetSquare).ToArray().Length == 0))
            .Union(collect_moves(board, false, KilobyteMoveType.promotion, move => true, true, move => move.IsPromotion))
            .Union(collect_moves(board, true, KilobyteMoveType.capture_prevent_promotion_cant_take, move =>
            {
                board.ForceSkipTurn();
                bool value = collect_moves(board, false, KilobyteMoveType.promotion, model => true, true, move => move.IsPromotion).ToArray().Length != 0;
                board.UndoSkipTurn();
                return value;
            }, true, move => board.GetLegalMoves(true).Where(test => test.TargetSquare == move.TargetSquare).ToArray().Length != 0))
            .Union(get_best_capture_moves(board))
            /*.Union(collect_moves(board, false, KilobyteMoveType.defend, move => true, true, move =>
            {
                Square[] opponent_capture_squares = board.GetLegalMoves(true).Select(opponent_takes => opponent_takes.TargetSquare).ToArray();
                board.UndoMove(move);
                bool defended = opponent_capture_squares.Where(hanging_square => move_defends_square(board, move, hanging_square)).ToArray().Length != 0;
                board.MakeMove(move);
                return defended && board.GetLegalMoves(true).Where(opponent_capture_move => opponent_capture_move.TargetSquare == move.TargetSquare).ToArray().Length == 0;
            }))*/
            .Union(collect_moves(board, false, KilobyteMoveType.castle, move => true, true, move => move.IsCastles))
            /*.Union(collect_moves(board, false, KilobyteMoveType.uncontested_apply_pressure, move => true, true, move => {
                bool cannot_be_taken = board.GetLegalMoves(true).Where(opponent_move => opponent_move.TargetSquare == move.TargetSquare).ToArray().Length == 0;
                board.ForceSkipTurn();
                bool applies_pressure = board.GetLegalMoves(true).Where(second_move => second_move.StartSquare == move.TargetSquare).ToArray().Length != 0;
                board.UndoSkipTurn();
                return cannot_be_taken && applies_pressure;
            }))*/
            .Union(board.GetLegalMoves().Select(move => new KilobyteMove(move, KilobyteMoveType.unknown)))
            .Union(collect_moves(board, false, KilobyteMoveType.get_out_of_check, move => board.IsInCheck(), true, move => !board.IsInCheck()).OrderBy(move => move.move.IsCapture))
            .Union(collect_moves(board, false, KilobyteMoveType.move_piece_to_safety, move => opponent_captures.Where(opponent_move => opponent_move.TargetSquare == move.StartSquare && (int)opponent_move.MovePieceType < (int)opponent_move.CapturePieceType).ToArray().Length != 0, true, move =>
                board.GetLegalMoves().Where(opponent_move => opponent_move.TargetSquare == move.TargetSquare).ToArray().Length == 0).OrderBy(legal_move => legal_move.move.IsCapture)
            )
            /*.Union(collect_moves(board, false, KilobyteMoveType.block_threatening_check_mate, move => true, true, move => {
                board.UndoMove(move);
                bool check_mate = get_check_mate_moves(board).ToArray().Length != 0;
                board.MakeMove(move);
                bool blocks_check_mate = get_check_mate_moves(board).ToArray().Length == 0;
                return check_mate && blocks_check_mate;
            }))*/
            //.Union(collect_moves(board, false, KilobyteMoveType.threatens_offensive_check_mate, move => true, true, move => board.IsInCheckmate()))
            /*.Union(collect_moves(board, false, KilobyteMoveType.development, move => true, true, move =>
            {
                bool uncontested = board.GetLegalMoves(true).Where(opponent_take => opponent_take.TargetSquare == move.TargetSquare).ToArray().Length == 0;
                return uncontested;
            }))*/
            .OrderBy(move => (int) move.move_type)
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
            Move[] opponent_captures = get_opponent_capturing_moves_on_moved_square(board, capture_move);
            return opponent_captures.Length == 0 // capture an undefended piece
            || opponent_captures.Where(opponent_capture => (int) opponent_capture.CapturePieceType <= (int) capture_move.CapturePieceType).ToArray().Length != 0 // trade same piece OR a lower tier for a higher tier
            ;
        })
            .Select(move => new KilobyteMove(move, KilobyteMoveType.capture))
            .ToHashSet();
    }

    Move[] get_opponent_capturing_moves_on_moved_square(Board board, Move after_move)
    {
        board.MakeMove(after_move);
        Move[] moves = board.GetLegalMoves(true).Where(move => move.TargetSquare == after_move.TargetSquare).ToArray();
        board.UndoMove(after_move);
        return moves;
    }
    Move[] get_opponent_attacking_moves(Board board)
    {
        board.ForceSkipTurn();
        Move[] moves = board.GetLegalMoves(true).ToArray();
        board.UndoSkipTurn();
        return moves;
    }
    Move[] get_piece_legal_moves(Board board, Square piece_square)
    {
        return board.GetLegalMoves().Where(move => move.StartSquare == piece_square).ToArray();
    }
    bool move_defends_square(Board board, Move move, Square square) // only works if the `square` is begin threatened
    {
        board.MakeMove(move);
        bool opponent_threatens = board.GetLegalMoves(true).Where(opponent_move => opponent_move.TargetSquare == square).ToArray().Length != 0;
        board.ForceSkipTurn();
        bool defends_square = board.GetLegalMoves(true).Where(second_move => second_move.StartSquare == square).ToArray().Length != 0;
        board.UndoSkipTurn();
        board.UndoMove(move);
        return opponent_threatens && defends_square;
    }
    Move[] get_check_mate_moves(Board board)
    {
        return board.GetLegalMoves().Where(move =>
        {
            board.MakeMove(move);
            bool check_mate = board.IsInCheckmate();
            board.UndoMove(move);
            return check_mate;
        }).ToArray();
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
        threatens_offensive_check_mate, // the bot is about to win
        block_threatening_check_mate,   // the bot is about to lose
        move_piece_to_safety,
        capture_prevent_promotion_cant_take,
        promotion,
        capture_cant_trade,
        castle,
        development,
        defend,
        capture,
        uncontested_apply_pressure,
        unknown
    }
}