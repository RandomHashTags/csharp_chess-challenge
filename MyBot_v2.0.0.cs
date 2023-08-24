///
/// Created by Evan Anderson on August 24, 2023.
/// v2.0.0
///

using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;

/// LAST BRAIN POWER: 289 (without debug info | MAX ALLOWED: 1024)
public class MyBot : IChessBot
{
    Move best_move, last_move;
    int[] piece_values = { 0, 100, 300, 300, 500, 3000, 10000 };
    int max_depth = 6;

    public Move Think(Board board, Timer timer)
    {
        evaluate_best_moves(board, max_depth, -999999, 999999, board.IsWhiteToMove ? 1 : -1);
        return best_move;
    }


    int evaluate_best_moves(Board board, int depth, int alpha, int beta, int color)
    {
        if (board.IsDraw())
        {
            return 0;
        } else if (board.IsInCheckmate())
        {
            int mate_in = max_depth - depth;
            if (mate_in != max_depth && mate_in == 2)
            {
                //Debug.WriteLine((color == 1 ? "White" : "Black") + " is checkmated in " + mate_in + " via " + last_move.ToString() + " (depth=" + depth + ")");
            }
            return int.MinValue;
        } else if (depth == 0)
        {
            // fallback by scoring material
            int material_score = 0;
            for (int i = 0; i < 7; i++)
            {
                PieceList my_pieces = board.GetPieceList((PieceType)i, true);
                PieceList opponent_pieces = board.GetPieceList((PieceType)i, false);
                material_score += ((my_pieces == null ? 0 : my_pieces.Count) - (opponent_pieces == null ? 0 : opponent_pieces.Count)) * piece_values[i];
            }
            return material_score * color;
        }
        Move[] moves = board.GetLegalMoves();

        int best_score = int.MinValue;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            last_move = move;
            int score = -evaluate_best_moves(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(move);

            if(score > best_score)
            {
                best_score = score;
                if (depth == max_depth)
                {
                    best_move = move;
                }
            }
            alpha = Math.Max(alpha, best_score);
            if (alpha >= beta)
            {
                break;
            }
        }
        return best_score;
    }
}