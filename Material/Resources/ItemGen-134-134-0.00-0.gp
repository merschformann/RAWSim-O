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
set output "ItemGen-134-134-0.00-0.pdf"
set title "ItemGen-134-134-0.00-0"
set xlabel "Frequency"
set ylabel "SKU count"
plot \
"ItemGen-134-134-0.00-0groups.dat" u 1:2 w boxes linestyle 1 t "SKU frequencies"
set title "ItemGen-134-134-0.00-0"
set xlabel "SKU"
set ylabel "frequency"
plot \
"ItemGen-134-134-0.00-0simple.dat" u 1:2 w steps linestyle 1 t "SKU frequencies"
set title "ItemGen-134-134-0.00-0"
set xlabel "SKU"
set ylabel "rel. cum. frequency"
plot \
"ItemGen-134-134-0.00-0cumulative.dat" u 1:2 w steps linestyle 1 t "cum. SKU frequencies"
set title "ItemGen-134-134-0.00-0"
set xlabel "SKU"
set ylabel "probability"
plot \
"ItemGen-134-134-0.00-0probability.dat" u 1:2 w steps linestyle 1 t "SKU probabilities"
set title "ItemGen-134-134-0.00-0"
set xlabel "SKU"
set ylabel "size"
plot \
"ItemGen-134-134-0.00-0weights.dat" u 1:2 w steps linestyle 1 t "SKU size"
set title "ItemGen-134-134-0.00-0"
set xlabel "SKU"
set ylabel "units"
plot \
"ItemGen-134-134-0.00-0bundlesizes.dat" u 1:2 w steps linestyle 1 t "SKU replenishment order size"
reset
exit
