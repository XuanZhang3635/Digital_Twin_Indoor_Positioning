% 读取文件
file1 = readtable('LeastSquaresSolution.csv'); 
% file2 = readtable('EstimateByVoxel_Gau_0.45_0.45_1800_3.csv');
% file3 = readtable('EstimateByVoxel_Gau_0.45_1_1800_3.csv');
% file4 = readtable('EstimateByVoxel_Gau_0.45_2.34_1800_3.csv');
% file5 = readtable('EstimateByVoxel_Gau_1_2.34_1800_3.csv');
% file6 = readtable('EstimateByVoxel_Gau_1_1_1800_3.csv');
% file7 = readtable('EstimateByVoxel_Gau_1_0.45_1800_3.csv');
% file8 = readtable('EstimateByVoxel_Gau_2.34_0.45_1800_3.csv');
% file9 = readtable('EstimateByVoxel_Gau_2.34_1_1800_3.csv');
% file10 = readtable('EstimateByVoxel_Gau_2.34_2.34_1800_3.csv');

% file11 = readtable('EstimateByVoxel_Gau_0.1_0.1_1800_3.csv');
% file12 = readtable('EstimateByVoxel_Gau_0.32_0.32_1800_3.csv');
% file13 = readtable('EstimateByVoxel_Gau_0.32_1_1800_3.csv');

file14 = readtable('EstimateByVoxel_Gau_0.2_1_1800_3.csv');
% file15 = readtable('EstimateByVoxel_Gau_0.7_1_1800_3.csv');

% file16 = readtable('EstimateByVoxel_Gau_0.05_1_1800_3.csv');
file17 = readtable('EstimateByVoxel_Gau_0.1_1_1800_3.csv');
% file18 = readtable('EstimateByVoxel_Gau_0.1_2_1800_3.csv');

% 提取误差列（假设列名都是 'Error'）
error1 = file1.Error;
% error2 = file2.Error;
% error3 = file3.Error;
% error4 = file4.Error;
% error5 = file5.Error;
% error6 = file6.Error;
% error7 = file7.Error;
% error8 = file8.Error;
% error9 = file9.Error;
% error10 = file10.Error;

% error11 = file11.Error;
% error12 = file12.Error;
% error13 = file13.Error;

error14 = file14.Error;
% error15 = file15.Error;

% error16 = file16.Error;
error17 = file17.Error;
% error18 = file18.Error;

% 绘制CDF
figure;
hold on;

[f1, x1] = ecdf(error1);
plot(x1, f1, '-o', 'DisplayName', 'Least Squares Solution');

% [f2, x2] = ecdf(error2);
% plot(x2, f2, '-o', 'DisplayName', 'EstimateByVoxel Gau 0.45 0.45');
% 
% [f3, x3] = ecdf(error3);
% plot(x3, f3, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.45 1');
% 
% [f4, x4] = ecdf(error4);
% plot(x4, f4, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.45 2.34');

% [f5, x5] = ecdf(error5);
% plot(x5, f5, '-o', 'DisplayName', 'EstimateByVoxel Gau 1 2.34');
% 
% [f6, x6] = ecdf(error6);
% plot(x6, f6, '-o', 'DisplayName', 'EstimateByVoxel Gau 1 1');
% 
% [f7, x7] = ecdf(error7);
% plot(x7, f7, '-x', 'DisplayName', 'EstimateByVoxel Gau 1 0.45');
% 
% [f8, x8] = ecdf(error8);
% plot(x8, f8, '-x', 'DisplayName', 'EstimateByVoxel Gau 2.34 0.45');
% 
% [f9, x9] = ecdf(error9);
% plot(x9, f9, '-x', 'DisplayName', 'EstimateByVoxel Gau 2.34 1');
% 
% [f10, x10] = ecdf(error10);
% plot(x10, f10, '-x', 'DisplayName', 'EstimateByVoxel Gau 2.34 2.34');
% 
% [f11, x11] = ecdf(error11);
% plot(x11, f11, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.1 0.1');
% 
% [f12, x12] = ecdf(error12);
% plot(x12, f12, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.32 0.32');
% 
% [f13, x13] = ecdf(error13);
% plot(x13, f13, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.32 1');

[f14, x14] = ecdf(error14);
plot(x14, f14, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.2 1');

% [f15, x15] = ecdf(error15);
% plot(x15, f15, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.7 1');
% 
% [f16, x16] = ecdf(error13);
% plot(x16, f16, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.05 1');

[f17, x17] = ecdf(error17);
plot(x17, f17, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.1 1');

% [f18, x18] = ecdf(error18);
% plot(x18, f18, '-x', 'DisplayName', 'EstimateByVoxel Gau 0.1 2');

% 添加标题和坐标轴标签
title('CDF Comparison: EstimateByVoxel vs Least Squares');
xlabel('Error (m)');
ylabel('Cumulative Probability');
legend('Location', 'southeast');
grid on;

% 保存图像
saveas(gcf, 'CDF_Comparison_EstimateByVoxel_LeastSquares.png');
