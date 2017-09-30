reset
# Output definition
set terminal pdfcairo enhanced size 7, 3 font "Consolas, 12"
set lmargin 13
set rmargin 13
# Parameters
set key right top Right
set grid
set style fill solid 0.75
# Line-Styles
set style line 1 linetype 1 linecolor rgb "#7090c8" linewidth 1
set output "Repo-1000.pdf"
set title "Repo-1000"
set xlabel "Frequency"
set ylabel "SKU count"
plot \
"Repo-1000groups.dat" u 1:2 w boxes linestyle 1 t "SKU frequencies"
set title "Repo-1000"
set xlabel "SKU"
set ylabel "frequency"
plot \
"Repo-1000simple.dat" u 1:2 w steps linestyle 1 t "SKU frequencies"
set title "Repo-1000"
set xlabel "SKU"
set ylabel "probability"
plot \
"Repo-1000probability.dat" u 1:2 w steps linestyle 1 t "SKU probabilities"
set title "Repo-1000"
set xlabel "SKU"
set ylabel "size"
plot \
"Repo-1000weights.dat" u 1:2 w steps linestyle 1 t "SKU size"
set title "Repo-1000"
set xlabel "SKU"
set ylabel "units"
plot \
"Repo-1000bundlesizes.dat" u 1:2 w steps linestyle 1 t "SKU replenishment order size"
reset
exit
