\ ================================================================
\  M O I R É   L I G H T S  —  Gforth terminal animation
\
\  Moiré interference between two animated concentric-ring
\  patterns, rendered in 256-colour ANSI on any VT100 terminal.
\
\  Run:   gforth moire.fs
\  Exit:  press any key  (or Ctrl-C)
\ ================================================================

80 constant COLS
24 constant ROWS

\ ─── integer print without trailing space ───────────────────────
: n.  ( n -- )  s>d <# #s #> type ;

\ ─── ANSI / VT100 terminal control ──────────────────────────────
: esc          27 emit ;
: cls          esc ." [2J" esc ." [H" ;
: at ( row col -- )  esc ." [" swap n. ." ;" n. ." H" ;
: fg256 ( n -- )     esc ." [38;5;" n. ." m" ;
: bg256 ( n -- )     esc ." [48;5;" n. ." m" ;
: reset        esc ." [0m" ;
: hide-cursor  esc ." [?25l" ;
: show-cursor  esc ." [?25h" ;

\ ─── integer square root — Newton's method ──────────────────────
\ Invariant: g is always >= floor(sqrt(u)).
\ Stop when the next Newton step g' >= g (we have converged).
: isqrt ( u -- r )
  dup 0= if exit then
  dup 2/ 1 max                       \ initial guess = max(u/2, 1)
  begin
    2dup / over + 2/                 \ g' = (g + u/g) / 2
    2dup swap >= if drop nip exit then  \ converged: return g
    nip                              \ iterate with g'
  again ;

\ ─── Euclidean distance ──────────────────────────────────────────
\ Saves cx and cy to the return stack so x,y remain accessible.
: dist ( x y cx cy -- d )
  >r >r                              \ r: cx on top, cy below
  swap r> -  dup *                   \ (x − cx)²
  swap r> -  dup *                   \ (y − cy)²
  + isqrt ;

\ ─── 256-colour palette: 16 steps, dark navy → spectrum → white ─
create pal
  16 ,  17 ,  18 ,  19 ,  20 ,  21 ,  \ 0-5   deepest navy
  27 ,  33 ,  39 ,  45 ,  51 ,        \ 6-10  blue → bright cyan
  82 , 226 , 220 , 196 , 231 ,        \ 11-15 green → yellow → red → white

: palette ( v -- color )  15 and  cells pal + @ ;

\ ─── per-pixel mutable state ─────────────────────────────────────
\ Using variables avoids complex stack-juggling across calls.
variable px   \ current column  (0-based)
variable py   \ current row     (0-based)
variable pt   \ animation tick

\ ─── moiré: XOR of two counter-phased concentric-ring patterns ──
\
\  Ring 1: centre at left quarter  (col COLS/4,  row ROWS/2)
\          phase advances  (+pt), rings appear to flow outward
\  Ring 2: centre at right quarter (col 3*COLS/4, row ROWS/2)
\          phase retreats  (-pt), rings appear to flow inward
\
\  The incommensurable wavelengths (9 vs 7) create the classic
\  moiré beat pattern across the screen.
: moire@ ( -- v )
  px @ py @  COLS 4 /     ROWS 2/  dist  pt @ +  9 mod
  px @ py @  COLS 3 4 */  ROWS 2/  dist  pt @ -  7 mod
  xor ;

\ ─── render one complete frame ───────────────────────────────────
\ Cursor is repositioned only once per row (24 times per frame)
\ so the inner 80 characters stream left-to-right naturally.
: draw-frame ( -- )
  ROWS 0 do                          \ i = row index (outer)
    i py !
    i 1+  1  at                      \ ESC[row;1H  — start of row
    COLS 0 do                        \ i = col index (inner), j = row
      i px !
      moire@  palette  dup fg256 bg256  bl emit
    loop
  loop ;

\ ─── main animation loop ─────────────────────────────────────────
: moire-demo ( -- )
  hide-cursor  cls
  0 pt !
  begin
    draw-frame
    pt @ 1+ pt !
    key?                             \ non-blocking check
  until
  key drop                           \ consume the keypress
  show-cursor  reset ;

moire-demo
bye
