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
set output "Delta-1000.pdf"
set title "Delta-1000"
set xlabel "Frequency"
set ylabel "SKU count"
plot \
"Delta-1000groups.dat" u 1:2 w boxes linestyle 1 t "SKU frequencies"
set title "Delta-1000"
set xlabel "SKU"
set ylabel "frequency"
plot \
"Delta-1000simple.dat" u 1:2 w steps linestyle 1 t "SKU frequencies"
set title "Delta-1000"
set xlabel "SKU"
set ylabel "probability"
plot \
"Delta-1000probability.dat" u 1:2 w steps linestyle 1 t "SKU probabilities"
set title "Delta-1000"
set xlabel "SKU"
set ylabel "size"
plot \
"Delta-1000weights.dat" u 1:2 w steps linestyle 1 t "SKU size"
set title "Delta-1000"
set xlabel "SKU"
set ylabel "units"
plot \
"Delta-1000bundlesizes.dat" u 1:2 w steps linestyle 1 t "SKU replenishment order size"
reset
exit
